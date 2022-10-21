using UnityEngine;

namespace Minecraft
{
    public static class CoordinateUtility
    {
        public static Vector3Int ToCoordinate(Vector3 position)
        {
            return Vector3Int.FloorToInt(position);
        }

        public static Vector3Int ToChunk(Vector3Int globalVoxelCoordinate)
        {
            return new Vector3Int
            {
                x = Mathf.FloorToInt(globalVoxelCoordinate.x / (float)Chunk.SIZE),
                y = Mathf.FloorToInt(globalVoxelCoordinate.y / (float)Chunk.SIZE),
                z = Mathf.FloorToInt(globalVoxelCoordinate.z / (float)Chunk.SIZE),
            };
        }

        public static Vector3Int ToLocal(Vector3Int globalVoxelCoordinate)
        {
            Vector3Int chunkCoordinate = ToChunk(globalVoxelCoordinate);
            return new Vector3Int
            {
                x = globalVoxelCoordinate.x - chunkCoordinate.x * Chunk.SIZE,
                y = globalVoxelCoordinate.y - chunkCoordinate.y * Chunk.SIZE,
                z = globalVoxelCoordinate.z - chunkCoordinate.z * Chunk.SIZE,
            };
        }

        public static Vector3Int ToLocal(Vector3Int chunkCoordinate, Vector3Int globalVoxelCoordinate)
        {
            return new Vector3Int
            {
                x = globalVoxelCoordinate.x - chunkCoordinate.x * Chunk.SIZE,
                y = globalVoxelCoordinate.y - chunkCoordinate.y * Chunk.SIZE,
                z = globalVoxelCoordinate.z - chunkCoordinate.z * Chunk.SIZE,
            };
        }

        public static Vector3Int ToGlobal(Vector3Int chunkCoordinate, Vector3Int localVoxelCoordinate)
        {
            return chunkCoordinate * Chunk.SIZE + localVoxelCoordinate;
        }
    }
}