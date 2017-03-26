using System;
using System.IO;

namespace WorldConverterTest
{
  class Chunk
  {
    static void Main(string[] args)
    {
      byte[,,] voxelData = new byte[16, 256, 16];

      byte[] bytes = File.ReadAllBytes("C:\\Users\\Benjamin\\Downloads\\Future CITY 4.1\\decompressed\\0.0.vdat");
      uint byteIndex = 0;
      for (uint x = 0; x < 16; ++x)
      {
        for (uint y = 0; y < 256; ++y)
        {
          for (uint z = 0; z < 16; ++z)
          {
            voxelData[x, y, z] = bytes[byteIndex];
            ++byteIndex;
          }
        }
      }

      Console.WriteLine(voxelData[0, 0, 0]);

      Console.Write("Press any key to continue...");
      Console.ReadKey();
    }
  }
}
