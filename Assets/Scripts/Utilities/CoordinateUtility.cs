using UnityEngine;

namespace Minecraft.Utilities {
    public static class CoordinateUtility {
        public static Vector3Int ToCoordinate(Vector3 position) {
            return Vector3Int.FloorToInt(position);
        }

        public static Vector3Int ToChunk(Vector3Int blockCoordinate) {
            return new Vector3Int {
                x = Mathf.FloorToInt(blockCoordinate.x / (float)Chunk.SIZE),
                y = Mathf.FloorToInt(blockCoordinate.y / (float)Chunk.SIZE),
                z = Mathf.FloorToInt(blockCoordinate.z / (float)Chunk.SIZE),
            };
        }

        public static Vector3Int ToLocal(Vector3Int chunkCoordinate, Vector3Int blockCoordinate) {
            return new Vector3Int {
                x = blockCoordinate.x - chunkCoordinate.x * Chunk.SIZE,
                y = blockCoordinate.y - chunkCoordinate.y * Chunk.SIZE,
                z = blockCoordinate.z - chunkCoordinate.z * Chunk.SIZE,
            };
        }

        public static Vector3Int ToGlobal(Vector3Int chunkCoordinate, Vector3Int localBlockCoordinate) {
            return chunkCoordinate * Chunk.SIZE + localBlockCoordinate;
        }
    }
}