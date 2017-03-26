# Minecraft Level Ripper
Takes a series of .mca files and outputs Minecraft block IDs at their Cartesian co-oridinates.

This is useful for people working on their own voxel implementations and need pre-built levels that they can easily use to debug their work.

# How to use
Simply download a minecraft map, unzip it, and drop it in the same directory as the executable.

Once the executable has finished running, you'll be left with a new folder containing folders for each dimension. Within each of these will be the uncompressed form of each Minecraft chunk.

Each chunk file totals exactly 65536 bytes in size. An example of how to read each chunk file can be see below.

TODO: Add example code
