# Minecraft Level Ripper
Takes a series of .mca files and outputs Minecraft block IDs at their Cartesian co-oridinates.

This is useful for people working on their own voxel implementations and need pre-built levels that they can easily use to debug their work.

# Instructions for use
Simply download a minecraft map, unzip it, and drag all the .mcr or .mcs files you want processed over the executable.

Once the executable has finished running, you'll be left with a new folder alongside the original files called 'decompressed' Within said folder will be a file for every chunk extracted containing the uncompressed voxel data.

Each chunk file totals exactly 65536 bytes in size. If the chunk consisted entirely of air or just didn't exist within the file, no file is written for it. An example of how to read each chunk file can be see below.

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
  
