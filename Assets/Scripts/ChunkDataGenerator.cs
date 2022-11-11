using Minecraft.Noise;
using Minecraft.Utilities;
using UnityEngine;

namespace Minecraft {
    public class ChunkDataGenerator : Singleton<ChunkDataGenerator> {
        [SerializeField] private Vector2 offset;
        [SerializeField, Min(0)] private int surfaceOffset = 64;
        [SerializeField, Min(1)] private int surfaceLevel = 64;
        [SerializeField, Min(0)] private int waterLevel = 45;
        [SerializeField] private Noise2D continentalnessNoise;
        [SerializeField] private Noise2D peaksAndValleysNoise;
        [SerializeField] private Noise2D erosionNoise;

        public ChunkData GenerateChunkData(Vector3Int coordinate) {
            ChunkData result = new() {
                Coordinate = coordinate
            };

            ChunkUtility.ForEachVoxel((x, y, z) => {
                Vector3Int globalVoxelCoordinate = CoordinateUtility.ToGlobal(coordinate, new Vector3Int(x, y, z));

                float erosionNoiseValue = erosionNoise.Sample(globalVoxelCoordinate.x, globalVoxelCoordinate.z, offset.x, offset.y);
                float continantalnessNoiseValue = continentalnessNoise.Sample(globalVoxelCoordinate.x, globalVoxelCoordinate.z, offset.x, offset.y);
                float peaksAndValleysNoiseValue = peaksAndValleysNoise.Sample(globalVoxelCoordinate.x, globalVoxelCoordinate.z, offset.x, offset.y);
                int surfaceHeight = surfaceOffset + Mathf.FloorToInt(continantalnessNoiseValue
                                      * peaksAndValleysNoiseValue
                                      * erosionNoiseValue
                                      * surfaceLevel);

                if (globalVoxelCoordinate.y > surfaceHeight) {
                    if (globalVoxelCoordinate.y <= waterLevel) {
                        result.BlockMap[x, y, z] = BlockType.Water;
                    } else {
                        result.BlockMap[x, y, z] = BlockType.Air;
                    }
                } else {
                    if (globalVoxelCoordinate.y >= surfaceHeight - 4) {
                        if (globalVoxelCoordinate.y <= waterLevel + 2) {
                            result.BlockMap[x, y, z] = BlockType.Sand;
                        } else {
                            if (globalVoxelCoordinate.y == surfaceHeight) {
                                result.BlockMap[x, y, z] = BlockType.Grass;
                            } else {
                                result.BlockMap[x, y, z] = BlockType.Dirt;
                            }
                        }
                    } else {
                        result.BlockMap[x, y, z] = BlockType.Stone;
                    }
                }
            });

            return result;
        }
    }
}