using Minecraft.Noise;
using UnityEngine;

namespace Minecraft
{
    public class ChunkDataGenerator : Singleton<ChunkDataGenerator>
    {
        [SerializeField] private Vector2 offset;
        [SerializeField, Min(0)] private int surfaceOffset = 64;
        [SerializeField, Min(1)] private int surfaceLevel = 64;
        [SerializeField, Min(0)] private int waterLevel = 45;
        [SerializeField] private Noise2D continentalnessNoise;
        [SerializeField] private Noise2D peaksAndValleysNoise;
        [SerializeField] private Noise2D erosionNoise;

        public ChunkData GenerateChunkData(Vector3Int coordinate)
        {
            ChunkData result = new()
            {
                Coordinate = coordinate
            };

            ChunkUtility.ForEachVoxel((x, y, z) => 
            {
                Vector3Int globalVoxelCoordinate = CoordinateUtility.ToGlobal(coordinate, new Vector3Int(x, y, z));

                float erosionNoiseValue = erosionNoise.Sample(globalVoxelCoordinate.x, globalVoxelCoordinate.z, offset.x, offset.y);
                float continantalnessNoiseValue = continentalnessNoise.Sample(globalVoxelCoordinate.x, globalVoxelCoordinate.z, offset.x, offset.y);
                float peaksAndValleysNoiseValue = peaksAndValleysNoise.Sample(globalVoxelCoordinate.x, globalVoxelCoordinate.z, offset.x, offset.y);
                int surfaceHeight = surfaceOffset + Mathf.FloorToInt(continantalnessNoiseValue
                                      * peaksAndValleysNoiseValue
                                      * erosionNoiseValue
                                      * surfaceLevel);

                if (globalVoxelCoordinate.y > surfaceHeight)
                {
                    if (globalVoxelCoordinate.y <= waterLevel)
                    {
                        result.VoxelMap[x, y, z] = VoxelType.Water;
                    }
                    else
                    {
                        result.VoxelMap[x, y, z] = VoxelType.Air;
                    }
                }
                else
                {
                    if (globalVoxelCoordinate.y >= surfaceHeight - 4)
                    {
                        if (globalVoxelCoordinate.y <= waterLevel + 2)
                        {
                            result.VoxelMap[x, y, z] = VoxelType.Sand;
                        }
                        else
                        {
                            if (globalVoxelCoordinate.y == surfaceHeight)
                            {
                                result.VoxelMap[x, y, z] = VoxelType.Grass;
                            }
                            else
                            {
                                result.VoxelMap[x, y, z] = VoxelType.Dirt;
                            }
                        }
                    }
                    else
                    {
                        result.VoxelMap[x, y, z] = VoxelType.Stone;
                    }
                }
            });

            return result;
        }
    }
}