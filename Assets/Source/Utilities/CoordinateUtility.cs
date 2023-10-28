using Minecraft.Components;
using Unity.Mathematics;

namespace Minecraft.Utilities {
    public static class CoordinateUtility {
        public static int3 ToChunk(in int3 voxelCoordinate) {
            return new int3 {
                x = (int)math.floor(voxelCoordinate.x / (float)Chunk.Size),
                y = (int)math.floor(voxelCoordinate.y / (float)Chunk.Size),
                z = (int)math.floor(voxelCoordinate.z / (float)Chunk.Size)
            };
        }

        public static int3 ToLocal(in int3 chunkCoordinate, in int3 voxelCoordinate) {
            return new int3 {
                x = voxelCoordinate.x - chunkCoordinate.x * Chunk.Size,
                y = voxelCoordinate.y - chunkCoordinate.y * Chunk.Size,
                z = voxelCoordinate.z - chunkCoordinate.z * Chunk.Size
            };
        }
    }
}