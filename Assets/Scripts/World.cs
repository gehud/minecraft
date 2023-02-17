using Minecraft.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Minecraft {
    public class World : MonoBehaviour {
        /// <summary>
        /// World height in Chunks.
        /// </summary>
        public const int HEIGHT = 16;

        public Dictionary<Vector3Int, Chunk> Chunks => chunks;

        public Dictionary<Vector3Int, ChunkRenderer> Renderers => renderers;

        public LightCalculator LightCalculatorSun { get; set; }

        public LightCalculator LightCalculatorRed { get; set; }

        public LightCalculator LightCalculatorGreen { get; set; }

        public LightCalculator LightCalculatorBlue { get; set; }

        public LiquidCalculator LiquidCalculatorWater { get; set; }

        [SerializeField]
        private new ChunkRenderer renderer;

        [SerializeField, Min(0)]
        private float tick = 0.25f;

        [Inject]
        private BlockDataProvider BlockDataProvider { get; }

        [Inject]
        private MaterialManager MaterialManager { get; }

        private Dictionary<Vector3Int, Chunk> chunks = new();
        private Dictionary<Vector3Int, ChunkRenderer> renderers = new();

		public bool HasChunk(Vector3Int coordinate) {
            return chunks.ContainsKey(coordinate);
		}

        public bool HasRenderer(Vector3Int coordinate) {
            return renderers.ContainsKey(coordinate);
		}

        public Chunk CreateChunk(Vector3Int coordinate) {
            var chunk = new Chunk();
            chunk.Coordinate = coordinate;
            chunks[coordinate] = chunk;
            return chunk;
        }

        public Chunk GetChunk(Vector3Int coordinate) {
            return chunks[coordinate];
		}

        public void SetChunk(Vector3Int coordinate, Chunk chunk) {
			chunks[coordinate] = chunk;
		}

        public ChunkRenderer CreateRenderer(Vector3Int coordinate) {
            var renderer = Instantiate(this.renderer, transform);
            var chunk = GetOrCreateChunk(coordinate);
            renderer.Initialize(chunk);
            renderers[coordinate] = renderer;
            return renderer;
        }

		public bool TryGetChunk(Vector3Int coordinate, out Chunk chunk) {
            return chunks.TryGetValue(coordinate, out chunk);
		}

		public bool TryGetRenderer(Vector3Int coordinate, out ChunkRenderer renderer) {
			return renderers.TryGetValue(coordinate, out renderer);
		}

		public Chunk GetOrCreateChunk(Vector3Int coordinate) {
            if (TryGetChunk(coordinate, out Chunk chunk))
                return chunk;
            return CreateChunk(coordinate);
        }

        public ChunkRenderer GetOrCreateRenderer(Vector3Int coordinate) {
            if (TryGetRenderer(coordinate, out ChunkRenderer renderer))
                return renderer;
            return CreateRenderer(coordinate);
        }

        public void DestroyChunk(Vector3Int coordinate) {
            chunks.Remove(coordinate);
        }

        public void DestroyRenderer(Vector3Int coordinate) {
            Destroy(renderers[coordinate].gameObject);
            renderers.Remove(coordinate);
        }

        /// <summary>
        /// Marks chunk as dirty if needed.
        /// </summary>
        public void ValidateChunk(Vector3Int chunkCoordinate, Vector3Int localBlockCoordinate) {
			if (localBlockCoordinate.x == 0 && TryGetChunk(chunkCoordinate + Vector3Int.left, out var chunk))
				chunk.MarkDirty();
			if (localBlockCoordinate.y == 0 && TryGetChunk(chunkCoordinate + Vector3Int.down, out chunk))
				chunk.MarkDirty();
			if (localBlockCoordinate.z == 0 && TryGetChunk(chunkCoordinate + Vector3Int.back, out chunk))
				chunk.MarkDirty();
			if (localBlockCoordinate.x == Chunk.SIZE - 1 && TryGetChunk(chunkCoordinate + Vector3Int.right, out chunk))
				chunk.MarkDirty();
			if (localBlockCoordinate.y == Chunk.SIZE - 1 && TryGetChunk(chunkCoordinate + Vector3Int.up, out chunk))
				chunk.MarkDirty();
			if (localBlockCoordinate.z == Chunk.SIZE - 1 && TryGetChunk(chunkCoordinate + Vector3Int.forward, out chunk))
				chunk.MarkDirty();
        }

		public BlockType GetBlock(Vector3Int blockCoordinate) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (TryGetChunk(chunkCoordinate, out Chunk chunk)) {
                Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                return chunk.BlockMap[localBlockCoordinate];
            }

            return BlockType.Air;
        }

        public BlockType GetBlock(int x, int y, int z) {
            return GetBlock(new Vector3Int(x, y, z));
        }

        public void SetBlock(Vector3Int blockCoordinate, BlockType voxelType) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (TryGetChunk(chunkCoordinate, out Chunk chunk)) {
                Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                chunk.BlockMap[localBlockCoordinate] = voxelType;
				chunk.MarkDirty();
				ValidateChunk(chunkCoordinate, localBlockCoordinate);
            }
        }

        public int GetLightLevel(Vector3Int blockCoordinate, LightChanel chanel) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (TryGetChunk(chunkCoordinate, out Chunk chunk)) {
                Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                return chunk.LightMap.Get(localBlockCoordinate, chanel);
            }

            return LightMap.MAX;
        }

        public byte GetLiquidAmount(Vector3Int blockCoordinate) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (TryGetChunk(chunkCoordinate, out Chunk chunk)) {
                Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                return chunk.LiquidMap[localBlockCoordinate].Amount;
            }

            return LiquidMap.MIN;
        }

		public void SetLiquidAmount(Vector3Int blockCoordinate, byte amount) {
			Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
			if (TryGetChunk(chunkCoordinate, out Chunk chunk)) {
				Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                chunk.LiquidMap.Set(localBlockCoordinate, amount);
				chunk.MarkDirty();
				ValidateChunk(chunkCoordinate, localBlockCoordinate);
			}
		}

		public byte GetLiquidAmount(Vector3Int blockCoordinate, BlockType type) {
			Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
			if (TryGetChunk(chunkCoordinate, out Chunk chunk)) {
				Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
				return chunk.LiquidMap.Get(localBlockCoordinate, type);
			}

			return LiquidMap.MIN;
		}

		public void SetLiquidAmount(Vector3Int blockCoordinate, BlockType type, byte amount) {
			Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
			if (TryGetChunk(chunkCoordinate, out Chunk chunk)) {
				Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
				chunk.LiquidMap.Set(localBlockCoordinate, type, amount);
                chunk.MarkDirty();
			    ValidateChunk(chunkCoordinate, localBlockCoordinate);
			}
		}

		public LiquidData GetLiquidData(Vector3Int blockCoordinate) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (TryGetChunk(chunkCoordinate, out Chunk chunk)) {
                Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                return chunk.LiquidMap[localBlockCoordinate];
            }

            return LiquidData.Empty;
        }

		public void DestroyVoxel(Vector3Int blockCoordinate) {
            SetBlock(blockCoordinate, BlockType.Air);
            LiquidCalculatorWater.Remove(blockCoordinate);

			LightCalculatorRed.Remove(blockCoordinate);
            LightCalculatorGreen.Remove(blockCoordinate);
            LightCalculatorBlue.Remove(blockCoordinate);
            LightCalculatorRed.Calculate();
            LightCalculatorGreen.Calculate();
            LightCalculatorBlue.Calculate();

            if (GetBlock(blockCoordinate + Vector3Int.up) == BlockType.Air
                && GetLightLevel(blockCoordinate + Vector3Int.up, LightChanel.Sun) == LightMap.MAX) {
                for (int y = blockCoordinate.y; y >= 0; y--) {
                    if (GetBlock(new Vector3Int(blockCoordinate.x, y, blockCoordinate.z)) != BlockType.Air)
                        break;
                    LightCalculatorSun.Add(blockCoordinate.x, y, blockCoordinate.z, LightMap.MAX);
                }
            }

            LiquidCalculatorWater.Add(blockCoordinate + Vector3Int.right);
            LiquidCalculatorWater.Add(blockCoordinate + Vector3Int.left);
            LiquidCalculatorWater.Add(blockCoordinate + Vector3Int.up);
            LiquidCalculatorWater.Add(blockCoordinate + Vector3Int.forward);
            LiquidCalculatorWater.Add(blockCoordinate + Vector3Int.back);

            LightCalculatorRed.Add(blockCoordinate + Vector3Int.right);
            LightCalculatorRed.Add(blockCoordinate + Vector3Int.left);
            LightCalculatorRed.Add(blockCoordinate + Vector3Int.up);
            LightCalculatorRed.Add(blockCoordinate + Vector3Int.down);
            LightCalculatorRed.Add(blockCoordinate + Vector3Int.forward);
            LightCalculatorRed.Add(blockCoordinate + Vector3Int.back);
            LightCalculatorRed.Calculate();

            LightCalculatorGreen.Add(blockCoordinate + Vector3Int.right);
            LightCalculatorGreen.Add(blockCoordinate + Vector3Int.left);
            LightCalculatorGreen.Add(blockCoordinate + Vector3Int.up);
            LightCalculatorGreen.Add(blockCoordinate + Vector3Int.down);
            LightCalculatorGreen.Add(blockCoordinate + Vector3Int.forward);
            LightCalculatorGreen.Add(blockCoordinate + Vector3Int.back);
            LightCalculatorGreen.Calculate();

            LightCalculatorBlue.Add(blockCoordinate + Vector3Int.right);
            LightCalculatorBlue.Add(blockCoordinate + Vector3Int.left);
            LightCalculatorBlue.Add(blockCoordinate + Vector3Int.up);
            LightCalculatorBlue.Add(blockCoordinate + Vector3Int.down);
            LightCalculatorBlue.Add(blockCoordinate + Vector3Int.forward);
            LightCalculatorBlue.Add(blockCoordinate + Vector3Int.back);
            LightCalculatorBlue.Calculate();

            LightCalculatorSun.Add(blockCoordinate + Vector3Int.right);
            LightCalculatorSun.Add(blockCoordinate + Vector3Int.left);
            LightCalculatorSun.Add(blockCoordinate + Vector3Int.up);
            LightCalculatorSun.Add(blockCoordinate + Vector3Int.down);
            LightCalculatorSun.Add(blockCoordinate + Vector3Int.forward);
            LightCalculatorSun.Add(blockCoordinate + Vector3Int.back);
            LightCalculatorSun.Calculate();
        }

        public void PlaceVoxel(Vector3Int blockCoordinate, BlockType voxelType) {
            LiquidCalculatorWater.Remove(blockCoordinate);

            SetBlock(blockCoordinate, voxelType);

            LightCalculatorRed.Remove(blockCoordinate);
            LightCalculatorGreen.Remove(blockCoordinate);
            LightCalculatorBlue.Remove(blockCoordinate);
            LightCalculatorSun.Remove(blockCoordinate);
            for (int y = blockCoordinate.y - 1; y >= 0; y--) {
                if (!BlockDataProvider.Get(GetBlock(new Vector3Int(blockCoordinate.x, y, blockCoordinate.z))).IsTransparent)
                    break;
                LightCalculatorSun.Remove(blockCoordinate.x, y, blockCoordinate.z);
            }
            LightCalculatorRed.Calculate();
            LightCalculatorGreen.Calculate();
            LightCalculatorBlue.Calculate();
            LightCalculatorSun.Calculate();

            LightColor emission = BlockDataProvider.Get(voxelType).Emission;
            if (emission.R != 0) {
                LightCalculatorRed.Add(blockCoordinate, emission.R);
                LightCalculatorRed.Calculate();
            }
            if (emission.G != 0) {
                LightCalculatorGreen.Add(blockCoordinate, emission.G);
                LightCalculatorGreen.Calculate();
            }
            if (emission.B != 0) {
                LightCalculatorBlue.Add(blockCoordinate, emission.B);
                LightCalculatorBlue.Calculate();
            }

            if (voxelType == BlockType.Water) {
                LiquidCalculatorWater.Add(blockCoordinate, LiquidMap.MAX);
            }
        }

        private void Awake() {
            LightCalculator.SetBlockDataManager(BlockDataProvider);
            LightCalculatorRed = new LightCalculator(this, LightChanel.Red);
            LightCalculatorGreen = new LightCalculator(this, LightChanel.Green);
            LightCalculatorBlue = new LightCalculator(this, LightChanel.Blue);
            LightCalculatorSun = new LightCalculator(this, LightChanel.Sun);
            LiquidCalculator.SetBlockDataManager(BlockDataProvider);
            LiquidCalculatorWater = new LiquidCalculator(this, BlockType.Water);
        }

        public void StartLiquidCalculation() => StartCoroutine(LiquidCalculation());

        private IEnumerator LiquidCalculation() {
            while (true) {
                LiquidCalculatorWater.Calculate();
                yield return new WaitForSeconds(tick);
            }
        }

        private void Update() {
            foreach (var renderer in renderers) {
                if (renderer.Value.Data.IsComplete && renderer.Value.Data.IsDirty)
                    renderer.Value.UpdateMesh(ChunkUtility.GenerateMeshData(this, renderer.Value.Data, BlockDataProvider), MaterialManager);
            }
        }
	}
}