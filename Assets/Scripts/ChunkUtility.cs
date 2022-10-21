using System;
using UnityEngine;

namespace Minecraft
{
    public static class ChunkUtility
    {
        public static void ForEachVoxel(Action<int, int, int> action)
        {
            for (int y = 0; y < Chunk.SIZE; y++)
                for (int x = 0; x < Chunk.SIZE; x++)
                    for (int z = 0; z < Chunk.SIZE; z++)
                        action(y, x, z);
        }

        public static void ForEachVoxel(Action<Vector3Int> action)
        {
            for (int y = 0; y < Chunk.SIZE; y++)
                for (int x = 0; x < Chunk.SIZE; x++)
                    for (int z = 0; z < Chunk.SIZE; z++)
                        action(new Vector3Int(x, y, z));
        }
    }
}