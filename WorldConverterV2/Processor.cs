using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace WorldConverter
{
  /// <summary>
  /// Basic .mca and .mcr file loader and processor.
  /// </summary>
  class Processor
  {
    /// <summary>
    /// Info regarding any one chunk within a .mca or .mcr file.
    /// </summary>
    private struct ChunkInfo
    {
      public int chunkX;
      public int chunkZ;
      public byte y;
      public byte[] voxelData;
    }

    /// <summary>
    /// X and Z dimensions of chunks within a .mca file.
    /// </summary>
    public const uint REGION_DIMENSIONS = 32;
    /// <summary>
    /// Size of a single sector in bytes (4Kb).
    /// </summary>
    public const uint SECTOR_SIZE = 4096;
    /// <summary>
    /// Z, Y and Z dimensions of each section within a chunk.
    /// </summary>
    public const uint SECTION_DIMENSIONS = 16;

    /// <summary>
    /// Loads a .mca file into memory from the given path before extracting, decompressing, and 
    /// writing all the chunk data to disk in the directory it was read from.   
    /// </summary>
    /// <param name="filePath">The full path to the file being processed.</param>
    public void ProcessFile(string filePath)
    {
      string parentDirectory = Directory.GetParent(filePath).ToString();
      byte[][] nbtChunksData = ReadFile(filePath);
      Chunk[,] chunks = ProcessData(nbtChunksData);
      for (uint x = 0; x < REGION_DIMENSIONS; ++x)
      {
        for (uint z = 0; z < REGION_DIMENSIONS; ++z)
        {
          //Writes out chunk data only if there is chunk data to be written.
          if (chunks[x, z] != null)
          {
            string newFilePath = string.Format("{0}\\decompressed\\{1}", 
              parentDirectory, Path.GetFileNameWithoutExtension(filePath));
            chunks[x, z].WriteData(x, z, newFilePath);
          }
        }
      }
    }
    
    /// <summary>
    /// Processes an array of NBT chunk data (in byte array form) to produce usable
    /// <see cref="Chunk"/>s.
    /// </summary>
    /// <param name="nbtChunksData">Chunk data in the form of an NBT file.</param>
    /// <returns>2D <see cref="Chunk"/> array filled with relevent data. Null chunks are empty.
    /// </returns>
    private Chunk[,] ProcessData(byte[][] nbtChunksData)
    {
      Chunk[,] chunks = new Chunk[REGION_DIMENSIONS, REGION_DIMENSIONS];
      //Loops through all the NBT chunk data to create the necessary chunks.
      for (uint chunkIndex = 0; chunkIndex < nbtChunksData.Length; ++chunkIndex)
      {
        byte[] bytes = nbtChunksData[chunkIndex];
        //Checks that there was actually some NBT data to read.
        if (bytes != null)
        {
          Chunk chunk = new Chunk();
          ChunkInfo chunkInfo = new ChunkInfo();
          chunkInfo.y = byte.MaxValue;

          uint byteIndex = 0;
          while (byteIndex < bytes.Length)
          {
            TagType tagType = (TagType)bytes[byteIndex];
            if (tagType > TagType.TAG_Int_Array)
            {
              throw new InvalidOperationException("Unrecognised TAG Type!");
            }

            //Skips over the end tag as it doesn't contain any useful information.
            if (tagType == TagType.TAG_End)
            {
               byteIndex += GetTagDataSize(tagType, null, 0);
            }
            else
            {
              uint tagNameLength = (uint)(bytes[byteIndex + 1] << 8) | bytes[byteIndex + 2];
              byte[] tagNameBytes = new byte[tagNameLength];
              Array.Copy(bytes, byteIndex + 3, tagNameBytes, 0, tagNameLength);
              string tagName = System.Text.Encoding.UTF8.GetString(tagNameBytes);
              byteIndex += 3 + tagNameLength;
              ExtractChunkInfo(bytes, byteIndex, chunk, ref chunkInfo, tagType, tagName);              

              //Steps into the contents of the list tag if it's the one we want.
              if (tagType == TagType.TAG_List && tagName == "Sections")
              {
                byteIndex += 5;
              }
              //Otherwise skip over the contents of any tag.
              else
              {
                byteIndex += GetTagDataSize(tagType, bytes, byteIndex);
              }
            }
          }
          chunks[REGION_DIMENSIONS - (Math.Abs(chunkInfo.chunkX) % REGION_DIMENSIONS) - 1, REGION_DIMENSIONS - (Math.Abs(chunkInfo.chunkZ) % REGION_DIMENSIONS) - 1] = chunk;
        }
      }
      return chunks;
    }

    /// <summary>
    /// Extracts information regarding the <see cref="Chunk"/> that this program seeks to output and
    /// updates the <see cref="ChunkInfo"/> object.
    /// </summary>
    /// <param name="bytes">The full .mca or .mcr file as a byte array.</param>
    /// <param name="byteIndex">The index within the <see cref="byte"/> array to access a tags
    /// contents</param>
    /// <param name="chunk">The <see cref="Chunk"/> to send voxel data to if applicable.</param>
    /// <param name="chunkInfo">The information to be set regarding a <see cref="Chunk"/> object.
    /// </param>
    /// <param name="tagType">The <see cref="TagType"/> being processed.</param>
    /// <param name="tagName">The name of the tag being processed.</param>
    private void ExtractChunkInfo(byte[] bytes, uint byteIndex,
                                  Chunk chunk, ref ChunkInfo chunkInfo,
                                  TagType tagType, string tagName)
    {
      if (tagType == TagType.TAG_Int)
      {
        if (tagName == "xPos")
        {
          chunkInfo.chunkX = (bytes[byteIndex] << 24) | (bytes[byteIndex + 1] << 16) |
            (bytes[byteIndex + 2] << 8) | bytes[byteIndex + 3];
        }
        else if (tagName == "zPos")
        {
          chunkInfo.chunkZ = (bytes[byteIndex] << 24) | (bytes[byteIndex + 1] << 16) | 
            (bytes[byteIndex + 2] << 8) | bytes[byteIndex + 3];
        }
      }
      //Writes chunk data to chunk only if all the data necessary to do so has been read.
      else if (tagType == TagType.TAG_Byte_Array && tagName == "Blocks")
      {
        chunkInfo.voxelData = new byte[SECTION_DIMENSIONS * SECTION_DIMENSIONS * SECTION_DIMENSIONS];
        for (uint i = 0; i < SECTION_DIMENSIONS * SECTION_DIMENSIONS * SECTION_DIMENSIONS; ++i)
        {
          chunkInfo.voxelData[i] = bytes[4 + byteIndex + i];
        }
        if (chunkInfo.y != byte.MaxValue)
        {
          chunk.SetVoxelData(chunkInfo.y, chunkInfo.voxelData);
          chunkInfo.y = byte.MaxValue;
          chunkInfo.voxelData = null;
        }
      }
      else if (tagType == TagType.TAG_Byte && tagName == "Y")
      {
        chunkInfo.y = bytes[byteIndex];
        if (chunkInfo.voxelData != null)
        {
          chunk.SetVoxelData(chunkInfo.y, chunkInfo.voxelData);
          chunkInfo.y = byte.MaxValue;
          chunkInfo.voxelData = null;
        }
      }
    }

    /// <summary>
    /// Calculates the size of the contents of a tag (with the exception of a TAG_Compound, where
    /// the size is considered 0).
    /// </summary>
    /// <param name="tagType">The tag to be examined.</param>
    /// <param name="bytes">The full .mca or .mcr file as a byte array.</param>
    /// <param name="byteIndex">The index within the <see cref="byte"/> array to access a tags
    /// contents</param>
    /// <returns>The size of the contents of a tag (with the exception of a TAG_Compound, where 0 is
    /// returned).</returns>
    private uint GetTagDataSize(TagType tagType, byte[] bytes, uint byteIndex)
    {
      uint tagDataSize;
      switch (tagType)
      {
        case TagType.TAG_End:
          tagDataSize = 1;
          break;
        case TagType.TAG_Byte:
          tagDataSize = 1;
          break;
        case TagType.TAG_Short:
          tagDataSize = 2;
          break;
        case TagType.TAG_Int:
          tagDataSize = 4;
          break;
        case TagType.TAG_Long:
          tagDataSize = 8;
          break;
        case TagType.TAG_Float:
          tagDataSize = 4;
          break;
        case TagType.TAG_Double:
          tagDataSize = 8;
          break;
        case TagType.TAG_Byte_Array:
          int byteArrayLength = (bytes[byteIndex] << 24) | (bytes[byteIndex + 1] << 16) | 
            (bytes[byteIndex + 2] << 8) | bytes[byteIndex + 3];
          tagDataSize = 4 + (uint)byteArrayLength;
          break;
        case TagType.TAG_String:
          int stringLength = (bytes[byteIndex] << 8) | bytes[byteIndex + 1];
          tagDataSize = 2 + (uint)stringLength;
          break;
        case TagType.TAG_List:
          TagType listTagType = (TagType)bytes[byteIndex];
          uint listTagDataSize = GetTagDataSize(listTagType, null, 0);
          int listLength = (bytes[byteIndex + 1] << 24) | (bytes[byteIndex + 2] << 16) | 
            (bytes[byteIndex + 3] << 8) | bytes[byteIndex + 4];
          tagDataSize = 5 + (uint)(listTagDataSize * listLength);          
          break;
        case TagType.TAG_Compound:
          //Just skip directly into the contents of the compound tag as it's size cannot be worked
          //from the information declaring the tag.
          tagDataSize = 0;
          break;
        case TagType.TAG_Int_Array:
          int intArrayLength = (bytes[byteIndex] << 24) | (bytes[byteIndex + 1] << 16) |
            (bytes[byteIndex + 2] << 8) | bytes[byteIndex + 3];
          tagDataSize = 4 + (uint)(intArrayLength * 4);
          break;
        default:
          throw new InvalidOperationException("Unrecognised TAG Type!");
      }
      return tagDataSize;
    }

    /// <summary>
    /// Reads a .mca or .mcr file containing up to 1024 chunks worth of data and decompresses them 
    /// to get the raw NBT chunk data.
    /// </summary>
    /// <param name="filePath">The full path to the file being processed.</param>
    /// <returns>An array of <see cref="byte"/> arrays representing NBT chunk data.</returns>
    private byte[][] ReadFile(string filePath)
    {
      byte[][] nbtChunkData = new byte[REGION_DIMENSIONS * REGION_DIMENSIONS][];
      byte[] bytes = File.ReadAllBytes(filePath);

      uint chunkIndex = 0;
      for (uint byteIndex = 0; byteIndex < REGION_DIMENSIONS * REGION_DIMENSIONS * 4; byteIndex += 4)
      {
        //Reads the header of the file for the chunks location.
        ulong chunkStart = (ulong)((bytes[byteIndex] << 16) |
          (bytes[byteIndex + 1] << 8) | bytes[byteIndex + 2]) * SECTOR_SIZE;
        if (chunkStart > 0)
        {
          uint chunkSize = (uint)((bytes[chunkStart] << 24) | (bytes[chunkStart + 1] << 16) | 
            (bytes[chunkStart + 2] << 8) | bytes[chunkStart + 3]) - 1;

          //Transfers the chunk specific data out of the byte array of the entire file.
          byte[] chunkData = new byte[chunkSize];
          for (uint i = 0; i < chunkSize; ++i)
          {
            chunkData[i] = bytes[chunkStart + 5 + i];
          }

          //Decompresses the chunk data into its raw NBT format.
          using (MemoryStream outputStream = new MemoryStream())
          {
            using (InflaterInputStream inputStream = 
              new InflaterInputStream(new MemoryStream(chunkData)))
            {
              inputStream.CopyTo(outputStream);
            }
            nbtChunkData[chunkIndex] = outputStream.ToArray();
          }
        }
        ++chunkIndex;
      }
      return nbtChunkData;
    }

    /// <summary>
    /// Entry point of the program.
    /// </summary>
    /// <param name="args">The paths for the files to be processed.</param>
    static void Main(string[] args)
    {
      Processor processor = new Processor();
      for (uint i = 0; i < args.Length; ++i)
      {
        if (args[i].EndsWith(".mcr") || args[i].EndsWith(".mca"))
        {
          Console.WriteLine(string.Format("Processing {0}...", args[i]));
          //Processes each valid file.
          processor.ProcessFile(args[i]);
        }
        else
        {
          Console.WriteLine(string.Format("Unsupported file format: {0}", Path.GetExtension(args[i])));
        }
      }

      Console.Write("Press any key to continue...");
      Console.ReadKey();
    }
  }
}
