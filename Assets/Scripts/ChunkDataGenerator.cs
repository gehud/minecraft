using Minecraft.Noise;
using Minecraft.Utilities;
using UnityEngine;

namespace Minecraft {
    public class ChunkDataGenerator : MonoBehaviour {
        [SerializeField] private Vector2 offset;
        [SerializeField, Min(0)] private int surfaceOffset = 64;
        [SerializeField, Min(0)] private int waterLevel = 45;
        [SerializeField] private Noise2D continentalnessNoise;
        [SerializeField] private Noise2D peaksAndValleysNoise;
        [SerializeField] private Noise2D erosionNoise;

        public ChunkData GenerateChunkData(Vector3Int coordinate) {
            ChunkData result = new() {
                Coordinate = coordinate
            };

            ChunkUtility.For((x, y, z) => {
                Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(coordinate, new Vector3Int(x, y, z));

                float erosionNoiseValue = erosionNoise.Sample(blockCoordinate.x, blockCoordinate.z, offset.x, offset.y);
                float continantalnessNoiseValue = continentalnessNoise.Sample(blockCoordinate.x, blockCoordinate.z, offset.x, offset.y);
                float peaksAndValleysNoiseValue = peaksAndValleysNoise.Sample(blockCoordinate.x, blockCoordinate.z, offset.x, offset.y);
                int surfaceHeight = surfaceOffset + Mathf.FloorToInt((continantalnessNoiseValue
                                                                    + peaksAndValleysNoiseValue
                                                                    + erosionNoiseValue) / 3);

                if (blockCoordinate.y > surfaceHeight) {
                    if (blockCoordinate.y <= waterLevel) {
                        result.BlockMap[x, y, z] = BlockType.Water;
                        result.LiquidMap.Set(x, y, z, BlockType.Water, LiquidMap.MAX);
                    } else {
                        result.BlockMap[x, y, z] = BlockType.Air;
                    }
                } else {
                    if (blockCoordinate.y >= surfaceHeight - 4) {
                        if (blockCoordinate.y <= waterLevel + 2) {
                            result.BlockMap[x, y, z] = BlockType.Sand;
                        } else {
                            if (blockCoordinate.y == surfaceHeight) {
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