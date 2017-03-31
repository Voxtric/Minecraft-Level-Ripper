using System.IO;

namespace WorldConverter
{
  /// <summary>
  /// Stores raw voxel information in the same size that it would exist in Minecraft.
  /// </summary>
  class Chunk
  {
    public const uint DIMENSION_X = 16;
    public const uint DIMENSION_Y = 256;
    public const uint DIMENSION_Z = 16;
    
    private byte[,,] m_voxelData;
    
    public Chunk()
    {
      //Initialises the voxel data array.
      m_voxelData = new byte[DIMENSION_X, DIMENSION_Y, DIMENSION_Z];
    }

    /// <summary>
    /// Sets the voxel data within the chunk from the voxel data provided in the NBT file at the
    /// correct Y co-ordinate.
    /// </summary>
    /// <param name="startY">The Y co-oridnate in terms of section size to start at.</param>
    /// <param name="voxelData">The <see cref="byte"/> array of information to transfer.</param>
    public void SetVoxelData(byte startY, byte[] voxelData)
    {
      //Loops through every voxel position, retrieving the right value from the 1D array and
      //storing it in the 3D voxel data array.
      for (uint x = 0; x < Processor.SECTION_DIMENSIONS; ++x)
      {
        for (uint y = 0; y < Processor.SECTION_DIMENSIONS; ++y)
        {
          for (uint z = 0; z < Processor.SECTION_DIMENSIONS; ++z)
          {
            //Calculates the correct index from the X, Y, and Z values provided.
            uint index = (y * Processor.SECTION_DIMENSIONS * Processor.SECTION_DIMENSIONS) + 
              (z * Processor.SECTION_DIMENSIONS) + x;

            //Sets the appropriate voxel at the correct position.
            m_voxelData[x, y + (startY * Processor.SECTION_DIMENSIONS), z] = voxelData[index];
          }
        }
      }
    }

    /// <summary>
    /// Writes the uncompressed chunk as a .vdat file to the same directory as where it was
    /// initially read from.
    /// </summary>
    /// <param name="chunkX">The X co-ordinate of the <see cref="Chunk"/>.</param>
    /// <param name="chunkZ">The Y co-ordinate of the <see cref="Chunk"/>.</param>
    /// <param name="directoryPath">The path to the directory to store all the .vdat files in.
    /// </param>
    public void WriteData(uint chunkX, uint chunkZ, string directoryPath)
    {
      string filePath = string.Format("{0}\\c.{1}.{2}.vdat", directoryPath, chunkX, chunkZ);
      Directory.CreateDirectory(directoryPath); //Ensures the directory always exists.

      //Reads bytes out of the chunk format into a byte array for writing to disk.
      byte[] bytes = new byte[DIMENSION_X * DIMENSION_Y * DIMENSION_Z];
      uint byteIndex = 0;
      for (uint x = 0; x < DIMENSION_X; ++x)
      {
        for (uint y = 0; y < DIMENSION_Y; ++y)
        {
          for (uint z = 0; z < DIMENSION_Z; ++z)
          {
            bytes[byteIndex] = m_voxelData[x, y, z];
            ++byteIndex;
          }
        }
      }
      File.WriteAllBytes(filePath, bytes);
    }
  }
}
