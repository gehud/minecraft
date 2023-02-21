using Minecraf;
using Minecraft.Noise;
using Minecraft.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Minecraft {
    public class ChunkGenerator : MonoBehaviour {
        [SerializeField] private Vector2 offset;
        [SerializeField, Min(0)] private int waterLevel = 32;
        [Header("Surface")]
        [SerializeField] private Noise2D continentalness;
        [SerializeField] private Noise2D peaksAndValleys;
        [SerializeField] private Noise2D erosion;
        [Header("Biomes")]
        [SerializeField] private Noise2D temperature;
        [SerializeField] private Noise2D humidity;
        [SerializeField] private Noise2D trees;
        [SerializeField] private Noise2D beachOffset;
        [SerializeField] private Noise2D waterDetails;
        [SerializeField] private Noise2D vegetation;

        [Inject]
        private readonly World world;

        public float GetContinentalness(Vector3Int blockCoordinate) {
            return continentalness.Sample(blockCoordinate.x, blockCoordinate.z, offset.x, offset.y);
		}

		public float GetErosion(Vector3Int blockCoordinate) {
			return erosion.Sample(blockCoordinate.x, blockCoordinate.z, offset.x, offset.y);
		}

		public float GetPeaksAndValleys(Vector3Int blockCoordinate) {
			return peaksAndValleys.Sample(blockCoordinate.x, blockCoordinate.z, offset.x, offset.y);
		}

		public float GetTemperature(Vector3Int blockCoordinate) {
			return temperature.Sample(blockCoordinate.x, blockCoordinate.z, offset.x, offset.y);
		}

        public float GetHumidity(Vector3Int blockCoordinate) {
            return humidity.Sample(blockCoordinate.x, blockCoordinate.z, offset.x, offset.y);
        }

		public Chunk Generate(Vector3Int coordinate) {
            Chunk result = world.CreateChunk(coordinate);

            var treePositions = GetLocalMaxima(GenerateColumnNoise(new Vector2Int(coordinate.x, coordinate.z), trees));

            ChunkUtility.For((x, y, z) => {
                Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(coordinate, new Vector3Int(x, y, z));

                float continantalness = GetContinentalness(blockCoordinate);
                float peaksAndValleys = GetPeaksAndValleys(blockCoordinate);
                float erosion = GetErosion(blockCoordinate);
                int surfaceHeight = Mathf.FloorToInt((continantalness + peaksAndValleys + erosion) / 3);

                float temperature = GetTemperature(blockCoordinate);
                float humidity = GetHumidity(blockCoordinate);

                BiomeType biome;
                if (temperature > 0.8f)
                    biome = BiomeType.Desert;
                else
                    biome = BiomeType.Forest;

                if (blockCoordinate.y > surfaceHeight) {
                    if (blockCoordinate.y <= waterLevel) {
                        result.BlockMap[x, y, z] = BlockType.Water;
                        result.LiquidMap.Set(x, y, z, BlockType.Water, LiquidMap.MAX);
                    } else {
                        result.BlockMap[x, y, z] = BlockType.Air;
                        if (biome != BiomeType.Desert && blockCoordinate.y == surfaceHeight + 1 && blockCoordinate.y > waterLevel + 3) {
                            if (treePositions.Contains(new Vector2Int(x, z)))
                                result.TreeData.Positions.Add(new Vector3Int(x, y, z));
                            else {
								var vegetation = this.vegetation.Sample(blockCoordinate.x, blockCoordinate.z, offset.x, offset.y);
								if (vegetation >= 0.5f) {
									if (vegetation <= 0.9f) {
										result.BlockMap[x, y, z] = BlockType.TallGrass;
									} else if (vegetation > 0.9f && vegetation <= 0.95f) {
										result.BlockMap[x, y, z] = BlockType.RedFlower;
									} else {
										result.BlockMap[x, y, z] = BlockType.YellowFlower;
									}
								}
							}
                        }
                    }
                } else {
                    if (blockCoordinate.y >= surfaceHeight - 5) {
                        if (biome == BiomeType.Desert) {
                            result.BlockMap[x, y, z] = BlockType.Sand;
                        } else {
                            float beachOffset = this.beachOffset.Sample(blockCoordinate.x, blockCoordinate.z, offset.x, offset.y);
                            if (blockCoordinate.y <= waterLevel + (int)(beachOffset * 3)) {
								float waterDetails = this.waterDetails.Sample(blockCoordinate.x, blockCoordinate.z, offset.x, offset.y);
								if (waterDetails <= 0.3f) {
                                    if (blockCoordinate.y == surfaceHeight && blockCoordinate.y > waterLevel - 1)
									    result.BlockMap[x, y, z] = BlockType.Grass;
                                    else
									    result.BlockMap[x, y, z] = BlockType.Dirt;
                                } else if (waterDetails <= 0.6f && waterDetails >= 0.3f)
									result.BlockMap[x, y, z] = BlockType.Gravel;
								else
									result.BlockMap[x, y, z] = BlockType.Sand;
							} else {
                                if (blockCoordinate.y == surfaceHeight) {
                                    result.BlockMap[x, y, z] = BlockType.Grass;
                                } else {
                                    result.BlockMap[x, y, z] = BlockType.Dirt;
                                }
                            }
                        }
                    } else {
                        result.BlockMap[x, y, z] = BlockType.Stone;
                    }
                }
            });

            return result;
        }

        public float[,] GenerateColumnNoise(Vector2Int column, Noise2D noise) {
			var result = new float[Chunk.SIZE, Chunk.SIZE];

			for (int x = 0; x < Chunk.SIZE; x++)
				for (int y = 0; y < Chunk.SIZE; y++)
					result[x, y] = noise.Sample(column.x * Chunk.SIZE + x,
                        column.y * Chunk.SIZE + y);

			return result;
		}

        private readonly Vector2Int[] checkDirections = new Vector2Int[] {
            new Vector2Int( 0,  1),
            new Vector2Int( 1,  1),
            new Vector2Int( 1,  0),
            new Vector2Int( 1, -1),
            new Vector2Int( 0, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1,  0),
            new Vector2Int(-1,  1),
        };

        private bool CheckNeighbours(float[,] data, int x, int y, Predicate<float> predicate) {
            foreach (var direction in checkDirections) {
                var position = new Vector2Int(x, y) + direction;

                if (position.x < 0 || position.x >= Chunk.SIZE || position.y < 0 || position.y >= Chunk.SIZE)
                    return false;

                if (!predicate(data[position.x, position.y]))
                    return false;
            }

            return true;
        }

        private List<Vector2Int> GetLocalMaxima(float[,] data) {
            var result = new List<Vector2Int>();
			for (int x = 0; x < Chunk.SIZE; x++)
				for (int y = 0; y < Chunk.SIZE; y++)
                    if (CheckNeighbours(data, x, y, neighbourNoise => neighbourNoise < data[x, y]))
                        result.Add(new Vector2Int(x, y));

            return result;
		}
    }
}