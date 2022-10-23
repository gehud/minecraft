using UnityEngine;

namespace Minecraft
{
    public class ChunkDataGenerator : Singleton<ChunkDataGenerator>
    {
        public ChunkData GenerateChunkData(Vector3Int coordinate)
        {
            ChunkData result = new()
            {
                Coordinate = coordinate
            };

            ChunkUtility.ForEachVoxel((x, y, z) => 
            {
                Vector3Int globalVoxelCoordinate = CoordinateUtility.ToGlobal(coordinate, new Vector3Int(x, y, z));
                if (globalVoxelCoordinate.y < 32)
                    result.VoxelMap[x, y, z] = VoxelType.Stone;
            });

            return result;
        }
    }
}