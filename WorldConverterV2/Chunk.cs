using System;
using System.IO;

namespace WorldConverterV2
{
  class Chunk
  {
    public const uint DIMESNION_X = 16;
    public const uint DIMENSION_Y = 256;
    public const uint DIMENSION_Z = 16;

    private byte[,,] m_voxelData;

    public Chunk()
    {
      m_voxelData = new byte[DIMESNION_X, DIMENSION_Y, DIMENSION_Z];
    }

    public void SetVoxelData(byte startY, byte[] voxelData)
    {
      for (uint x = 0; x < Loader.SECTION_DIMENSIONS; ++x)
      {
        for (uint y = 0; y < Loader.SECTION_DIMENSIONS; ++y)
        {
          for (uint z = 0; z < Loader.SECTION_DIMENSIONS; ++z)
          {
            uint index = (y * Loader.SECTION_DIMENSIONS * Loader.SECTION_DIMENSIONS) + (z * Loader.SECTION_DIMENSIONS) + x;
            m_voxelData[x, y + (startY * Loader.SECTION_DIMENSIONS), z] = voxelData[index];
          }
        }
      }
    }

    public void WriteData(uint chunkX, uint chunkZ, string directoryPath)
    {
      string filePath = string.Format("{0}\\{1}.{2}.vdat", directoryPath, chunkX, chunkZ);
      Directory.CreateDirectory(directoryPath);
      byte[] bytes = new byte[DIMESNION_X * DIMENSION_Y * DIMENSION_Z];
      uint byteIndex = 0;
      for (uint x = 0; x < DIMESNION_X; ++x)
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
      Console.WriteLine(string.Format("Wrote file for chunk at {0}, {1}", chunkX, chunkZ));
    }
  }
}
