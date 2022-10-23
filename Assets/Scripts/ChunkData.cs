using UnityEngine;

namespace Minecraft
{
    public class ChunkData
    {
        public Vector3Int Coordinate;
        public VoxelMap VoxelMap;

        public ChunkData()
        {
            Coordinate = Vector3Int.zero;
            VoxelMap = new();
        }

        public ChunkData(Vector3Int coordinate)
        {
            Coordinate = coordinate;
            VoxelMap = new();
        }

        public ChunkData(Vector3Int coordinate, VoxelMap voxelMap)
        {
            Coordinate = coordinate;
            VoxelMap = voxelMap;
        }
    }
}