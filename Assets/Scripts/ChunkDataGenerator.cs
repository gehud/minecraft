using Minecraft.Noise;
using Minecraft.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft {
    public class ChunkDataGenerator : MonoBehaviour {
        [SerializeField] private Vector2 offset;
        [SerializeField, Min(0)] private int surfaceOffset = 64;
        [SerializeField, Min(0)] private int waterLevel = 45;
        [SerializeField] private Noise2D continentalnessNoise;
        [SerializeField] private Noise2D peaksAndValleysNoise;
        [SerializeField] private Noise2D erosionNoise;
        [SerializeField] private Noise2D treeNoise;

        public float GetContinentalness(Vector3Int blockCoordinate) {
            return continentalnessNoise.Sample(blockCoordinate.x, blockCoordinate.z, offset.x, offset.y);
		}

		public float GetErosion(Vector3Int blockCoordinate) {
			return erosionNoise.Sample(blockCoordinate.x, blockCoordinate.z, offset.x, offset.y);
		}

		public float GetPeaksAndValleys(Vector3Int blockCoordinate) {
			return peaksAndValleysNoise.Sample(blockCoordinate.x, blockCoordinate.z, offset.x, offset.y);
		}

		public ChunkData GenerateChunkData(Vector3Int coordinate) {
            ChunkData result = new() {
                Coordinate = coordinate
            };

            var treePositions = GetLocalMaxima(GenerateTreeNoise(new Vector2Int(coordinate.x, coordinate.z)));

            ChunkUtility.For((x, y, z) => {
                Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(coordinate, new Vector3Int(x, y, z));

                float continantalnessNoiseValue = GetContinentalness(blockCoordinate);
                float peaksAndValleysNoiseValue = GetPeaksAndValleys(blockCoordinate);
                float erosionNoiseValue = GetErosion(blockCoordinate);
                int surfaceHeight = surfaceOffset + Mathf.FloorToInt((continantalnessNoiseValue
                                                                    + peaksAndValleysNoiseValue
                                                                    + erosionNoiseValue) / 3);
                if (blockCoordinate.y > surfaceHeight) {
                    if (blockCoordinate.y <= waterLevel) {
                        result.BlockMap[x, y, z] = BlockType.Water;
                        result.LiquidMap.Set(x, y, z, BlockType.Water, LiquidMap.MAX);
                    } else {
                        result.BlockMap[x, y, z] = BlockType.Air;
                        if (blockCoordinate.y == surfaceHeight + 1 
                        && blockCoordinate.y > waterLevel + 3
                        && treePositions.Contains(new Vector2Int(x, z))) {
                            result.TreeData.Positions.Add(new Vector3Int(x, y, z));
                        }
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

        private float[,] GenerateTreeNoise(Vector2Int chunkColumn) {
            var result = new float[Chunk.SIZE, Chunk.SIZE];

            for (int x = 0; x < Chunk.SIZE; x++)
                for (int y = 0; y < Chunk.SIZE; y++)
                    result[x, y] = treeNoise.Sample(chunkColumn.x * Chunk.SIZE + x, 
                                                    chunkColumn.y * Chunk.SIZE + y);

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