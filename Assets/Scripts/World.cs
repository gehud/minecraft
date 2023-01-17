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

        public Dictionary<Vector3Int, ChunkData> ChunksData { get; set; } = new();

        public Dictionary<Vector3Int, Chunk> Chunks { get; set; } = new();

        public LightCalculator LightCalculatorSun { get; set; }

        public LightCalculator LightCalculatorRed { get; set; }

        public LightCalculator LightCalculatorGreen { get; set; }

        public LightCalculator LightCalculatorBlue { get; set; }

        public LiquidCalculator LiquidCalculatorWater { get; set; }

        [SerializeField]
        private Chunk chunk;

        [SerializeField, Min(0)]
        private float tick = 0.25f;

        [Inject]
        private BlockDataManager BlockDataManager { get; }

        [Inject]
        private MaterialManager MaterialManager { get; }

        public ChunkData CreateChunkData(Vector3Int blockCoordinate) {
            var chunkData = new ChunkData {
                Coordinate = blockCoordinate
            };
            ChunksData.Add(blockCoordinate, chunkData);
            return chunkData;
        }

        public Chunk CreateChunk(Vector3Int blockCoordinate) {
            var chunk = Instantiate(this.chunk, transform);
            var chunkData = GetOrCreateChunkData(blockCoordinate);
            chunk.Initialize(chunkData);
            Chunks.Add(blockCoordinate, chunk);
            return chunk;
        }

        public ChunkData GetChunkData(Vector3Int blockCoordinate) {
            if (ChunksData.ContainsKey(blockCoordinate))
                return ChunksData[blockCoordinate];

            return null;
        }

        public Chunk GetChunk(Vector3Int blockCoordinate) {
            if (Chunks.ContainsKey(blockCoordinate))
                return Chunks[blockCoordinate];

            return null;
        }

        public ChunkData GetOrCreateChunkData(Vector3Int blockCoordinate) {
            if (ChunksData.ContainsKey(blockCoordinate))
                return ChunksData[blockCoordinate];

            return CreateChunkData(blockCoordinate);
        }

        public Chunk GetOrCreateChunk(Vector3Int blockCoordinate) {
            if (Chunks.ContainsKey(blockCoordinate))
                return Chunks[blockCoordinate];

            return CreateChunk(blockCoordinate);
        }

        public void DestroyChunk(Vector3Int blockCoordinate) {
            ChunksData.Remove(blockCoordinate);
            Chunks.Remove(blockCoordinate, out Chunk chunk);
            Destroy(chunk.gameObject);
        }

        /// <summary>
        /// Marks chunk data as dirty if needed.
        /// </summary>
        public void ValidateChunkData(Vector3Int chunkCoordinate, Vector3Int localBlockCoordinate) {
			ChunkData chunkData;
            if (localBlockCoordinate.x == 0 && ChunksData.TryGetValue(chunkCoordinate + Vector3Int.left, out chunkData))
                chunkData.MarkDirty();
			if (localBlockCoordinate.y == 0 && ChunksData.TryGetValue(chunkCoordinate + Vector3Int.down, out chunkData))
				chunkData.MarkDirty();
			if (localBlockCoordinate.z == 0 && ChunksData.TryGetValue(chunkCoordinate + Vector3Int.back, out chunkData))
				chunkData.MarkDirty();
			if (localBlockCoordinate.x == Chunk.SIZE - 1 && ChunksData.TryGetValue(chunkCoordinate + Vector3Int.right, out chunkData))
				chunkData.MarkDirty();
			if (localBlockCoordinate.y == Chunk.SIZE - 1 && ChunksData.TryGetValue(chunkCoordinate + Vector3Int.up, out chunkData))
				chunkData.MarkDirty();
			if (localBlockCoordinate.z == Chunk.SIZE - 1 && ChunksData.TryGetValue(chunkCoordinate + Vector3Int.forward, out chunkData))
				chunkData.MarkDirty();
        }

		public BlockType GetBlock(Vector3Int blockCoordinate) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                return chunkData.BlockMap[localBlockCoordinate];
            }

            return BlockType.Air;
        }

        public void SetBlock(Vector3Int blockCoordinate, BlockType voxelType) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                chunkData.BlockMap[localBlockCoordinate] = voxelType;
				chunkData.MarkDirty();
				ValidateChunkData(chunkCoordinate, localBlockCoordinate);
            }
        }

        public int GetLightLevel(Vector3Int blockCoordinate, LightChanel chanel) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                return chunkData.LightMap.Get(localBlockCoordinate, chanel);
            }

            return LightMap.MAX;
        }

        public byte GetLiquidAmount(Vector3Int blockCoordinate) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                return chunkData.LiquidMap[localBlockCoordinate].Amount;
            }

            return LiquidMap.MIN;
        }

		public void SetLiquidAmount(Vector3Int blockCoordinate, byte amount) {
			Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
			if (ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
				Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                chunkData.LiquidMap.Set(localBlockCoordinate, amount);
				chunkData.MarkDirty();
				ValidateChunkData(chunkCoordinate, localBlockCoordinate);
			}
		}

		public byte GetLiquidAmount(Vector3Int blockCoordinate, BlockType type) {
			Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
			if (ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
				Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
				return chunkData.LiquidMap.Get(localBlockCoordinate, type);
			}

			return LiquidMap.MIN;
		}

		public void SetLiquidAmount(Vector3Int blockCoordinate, BlockType type, byte amount) {
			Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
			if (ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
				Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
				chunkData.LiquidMap.Set(localBlockCoordinate, type, amount);
                chunkData.MarkDirty();
			    ValidateChunkData(chunkCoordinate, localBlockCoordinate);
			}
		}

		public LiquidData GetLiquidData(Vector3Int blockCoordinate) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                return chunkData.LiquidMap[localBlockCoordinate];
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
            for (int y = blockCoordinate.y; y >= 0; y--) {
                if (!BlockDataManager.Data[GetBlock(new Vector3Int(blockCoordinate.x, y, blockCoordinate.z))].IsTransparent)
                    break;
                LightCalculatorSun.Remove(blockCoordinate.x, y, blockCoordinate.z);
            }
            LightCalculatorRed.Calculate();
            LightCalculatorGreen.Calculate();
            LightCalculatorBlue.Calculate();
            LightCalculatorSun.Calculate();

            LightColor emission = BlockDataManager.Data[voxelType].Emission;
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
            LightCalculator.SetBlockDataManager(BlockDataManager);
            LightCalculatorRed = new LightCalculator(this, LightChanel.Red);
            LightCalculatorGreen = new LightCalculator(this, LightChanel.Green);
            LightCalculatorBlue = new LightCalculator(this, LightChanel.Blue);
            LightCalculatorSun = new LightCalculator(this, LightChanel.Sun);
            LiquidCalculator.SetBlockDataManager(BlockDataManager);
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
            foreach (var chunk in Chunks.Values) {
                if (chunk.Data.IsComplete && chunk.Data.IsDirty)
                    chunk.UpdateMesh(ChunkUtility.GenerateMeshData(this, chunk.Data, BlockDataManager), MaterialManager);
            }
        }
    }
}