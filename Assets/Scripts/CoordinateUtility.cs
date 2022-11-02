using UnityEngine;

namespace Minecraft {
    public static class CoordinateUtility {
        public static Vector3Int ToCoordinate(Vector3 position) {
            return Vector3Int.FloorToInt(position);
        }

        public static Vector3Int ToChunk(Vector3Int voxelCoordinate) {
            return new Vector3Int {
                x = Mathf.FloorToInt(voxelCoordinate.x / (float)Chunk.SIZE),
                y = Mathf.FloorToInt(voxelCoordinate.y / (float)Chunk.SIZE),
                z = Mathf.FloorToInt(voxelCoordinate.z / (float)Chunk.SIZE),
            };
        }

        public static Vector3Int ToLocal(Vector3Int voxelCoordinate) {
            Vector3Int chunkCoordinate = ToChunk(voxelCoordinate);
            return new Vector3Int {
                x = voxelCoordinate.x - chunkCoordinate.x * Chunk.SIZE,
                y = voxelCoordinate.y - chunkCoordinate.y * Chunk.SIZE,
                z = voxelCoordinate.z - chunkCoordinate.z * Chunk.SIZE,
            };
        }

        public static Vector3Int ToLocal(Vector3Int chunkCoordinate, Vector3Int voxelCoordinate) {
            return new Vector3Int {
                x = voxelCoordinate.x - chunkCoordinate.x * Chunk.SIZE,
                y = voxelCoordinate.y - chunkCoordinate.y * Chunk.SIZE,
                z = voxelCoordinate.z - chunkCoordinate.z * Chunk.SIZE,
            };
        }

        public static Vector3Int ToGlobal(Vector3Int chunkCoordinate, Vector3Int localVoxelCoordinate) {
            return chunkCoordinate * Chunk.SIZE + localVoxelCoordinate;
        }
    }
}