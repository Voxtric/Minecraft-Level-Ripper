using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace WorldConverterV2
{
  class Loader
  {
    public const uint REGION_DIMENSIONS = 32;
    public const uint SECTOR_SIZE = 1024 * 4;
    public const uint SECTION_DIMENSIONS = 16;

    Loader()
    {
      byte[][] nbtChunksData = ReadFile("C:\\Users\\Benjamin\\Downloads\\Future CITY 4.1\\region\\r.0.0.mca");
      Chunk[,] chunks = ProcessData(nbtChunksData);
      for (uint x = 0; x < REGION_DIMENSIONS; ++x)
      {
        for (uint z = 0; z < REGION_DIMENSIONS; ++z)
        {
          chunks[x, z].WriteData(x, z, "C:\\Users\\Benjamin\\Downloads\\Future CITY 4.1\\decompressed");
        }
      }
    }
    
    Chunk[,] ProcessData(byte[][] nbtChunksData)
    {
      Chunk[,] chunks = new Chunk[REGION_DIMENSIONS, REGION_DIMENSIONS];
      for (uint chunkIndex = 0; chunkIndex < nbtChunksData.Length; ++chunkIndex)
      {
        byte[] bytes = nbtChunksData[chunkIndex];
        Chunk chunk = new Chunk();
        int chunkX = 0;
        int chunkZ = 0;
        byte y = byte.MaxValue;
        byte[] voxelData = null;

        uint byteIndex = 0;
        while (byteIndex < bytes.Length)
        {
          TagType tagType = (TagType)bytes[byteIndex];
          if (tagType > TagType.TAG_Int_Array)
          {
            throw new InvalidOperationException("Unrecognised TAG Type!");
          }

          if (tagType == TagType.TAG_End)
          {
            ++byteIndex;
          }
          else
          {
            uint tagNameLength = (uint)(bytes[byteIndex + 1] << 8) | bytes[byteIndex + 2];
            byte[] tagNameBytes = new byte[tagNameLength];
            Array.Copy(bytes, byteIndex + 3, tagNameBytes, 0, tagNameLength);
            string tagName = System.Text.Encoding.UTF8.GetString(tagNameBytes);
            byteIndex += 3 + tagNameLength;

            //Personal processing.
            if (tagType == TagType.TAG_Int)
            {
              if (tagName == "xPos")
              {
                chunkX = (bytes[byteIndex] << 24) | (bytes[byteIndex + 1] << 16) | (bytes[byteIndex + 2] << 8) | bytes[byteIndex + 3];
              }
              else if (tagName == "zPos")
              {
                chunkZ = (bytes[byteIndex] << 24) | (bytes[byteIndex + 1] << 16) | (bytes[byteIndex + 2] << 8) | bytes[byteIndex + 3];
              }
            }
            else if (tagType == TagType.TAG_Byte_Array && tagName == "Blocks")
            {
              voxelData = new byte[SECTION_DIMENSIONS * SECTION_DIMENSIONS * SECTION_DIMENSIONS];
              for (uint i = 0; i < SECTION_DIMENSIONS * SECTION_DIMENSIONS * SECTION_DIMENSIONS; ++i)
              {
                voxelData[i] = bytes[4 + byteIndex + i];
              }
              if (y != byte.MaxValue)
              {
                chunk.SetVoxelData(y, voxelData);
                y = byte.MaxValue;
                voxelData = null;
              }
            }
            else if (tagType == TagType.TAG_Byte && tagName == "Y")
            {
              y = bytes[byteIndex];
              if (voxelData != null)
              {
                chunk.SetVoxelData(y, voxelData);
                y = byte.MaxValue;
                voxelData = null;
              }
            }
            
            //Skips the generally useless list tag.
            if (tagType == TagType.TAG_List && tagName == "Sections")
            {
              byteIndex += 5;
            }
            else
            {
              byteIndex += GetTagDataSize(tagType, bytes, byteIndex);
            }
          }
        }
        chunks[chunkX, chunkZ] = chunk;
        Console.WriteLine(String.Format("Read NBT data for chunk at {0}, {1}", chunkX, chunkZ));
      }
      return chunks;
    }

    public uint GetTagDataSize(TagType tagType, byte[] bytes, uint byteIndex)
    {
      uint tagDataSize;
      switch (tagType)
      {
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
          int byteArrayLength = (bytes[byteIndex] << 24) | (bytes[byteIndex + 1] << 16) | (bytes[byteIndex + 2] << 8) | bytes[byteIndex + 3];
          tagDataSize = 4 + (uint)byteArrayLength;
          break;
        case TagType.TAG_String:
          int stringLength = (bytes[byteIndex] << 8) | bytes[byteIndex + 1];
          tagDataSize = 2 + (uint)stringLength;
          break;
        case TagType.TAG_List:
          TagType listTagType = (TagType)bytes[byteIndex];
          uint listTagDataSize = GetTagDataSize(listTagType, null, 0);
          int listLength = (bytes[byteIndex + 1] << 24) | (bytes[byteIndex + 2] << 16) | (bytes[byteIndex + 3] << 8) | bytes[byteIndex + 4];
          tagDataSize = 5 + (uint)(listTagDataSize * listLength);          
          break;
        case TagType.TAG_Int_Array:
          int intArrayLength = (bytes[byteIndex] << 24) | (bytes[byteIndex + 1] << 16) | (bytes[byteIndex + 2] << 8) | bytes[byteIndex + 3];
          tagDataSize = 4 + (uint)(intArrayLength * 4);
          break;
        case TagType.TAG_End:
          tagDataSize = 1;
          break;
        default:
          tagDataSize = 0;
          break;
      }
      return tagDataSize;
    }

    public byte[][] ReadFile(string filePath)
    {
      byte[][] nbtChunkData = new byte[REGION_DIMENSIONS * REGION_DIMENSIONS][];
      byte[] bytes = File.ReadAllBytes(filePath);

      uint j = 0;
      for (uint byteIndex = 0; byteIndex < REGION_DIMENSIONS * REGION_DIMENSIONS * 4; byteIndex += 4)
      {
        ulong chunkStart = (ulong)((bytes[byteIndex] << 16) | (bytes[byteIndex + 1] << 8) | bytes[byteIndex + 2]) * SECTOR_SIZE;

        uint chunkSize = (uint)((bytes[chunkStart] << 24) | (bytes[chunkStart + 1] << 16) | (bytes[chunkStart + 2] << 8) | bytes[chunkStart + 3]) - 1;

        byte[] chunkData = new byte[chunkSize];
        for (uint i = 0; i < chunkSize; ++i)
        {
          chunkData[i] = bytes[chunkStart + 5 + i];
        }
        
        using (MemoryStream outputStream = new MemoryStream())
        {
          using (InflaterInputStream inputStream = new InflaterInputStream(new MemoryStream(chunkData)))
          {
            inputStream.CopyTo(outputStream);
          }
          nbtChunkData[j] = outputStream.ToArray();
        }
        Console.WriteLine(String.Format("Decompressed chunk {0}", j));
        ++j;
      }
      return nbtChunkData;
    }

    static void Main(string[] args)
    {
      Loader loader = new Loader();

      Console.Write("Press any key to continue...");
      Console.ReadKey();
    }
  }
}
