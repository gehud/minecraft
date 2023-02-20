using Minecraft.Utilities;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Minecraft {
    public class World : MonoBehaviour {
        /// <summary>
        /// World height in Chunks.
        /// </summary>
        public const int HEIGHT = 16;

        public int DrawDistance => drawDistance;

        private static int drawDistance = 2;

        public Vector2Int Center {
            get => center;
            set {
                UpdateCenter(value);
            }
        }

        private Vector2Int center = Vector2Int.zero;

        public int RenderersSize => DrawDistance * 2 + 1;

        public int ChunksSize => DrawDistance * 2 + 3;

        public int RenderersVolume => RenderersSize * RenderersSize * HEIGHT;

        public int ChunksVolume => ChunksSize * ChunksSize * HEIGHT;

        public LightCalculator LightCalculatorSun { get; set; }

        public LightCalculator LightCalculatorRed { get; set; }

        public LightCalculator LightCalculatorGreen { get; set; }

        public LightCalculator LightCalculatorBlue { get; set; }

        public LiquidCalculator LiquidCalculatorWater { get; set; }

        [SerializeField]
        private new ChunkRenderer renderer;
        [SerializeField]
        private bool debugChunks = false;

        [SerializeField, Min(0)]
        private float tick = 0.25f;

        [Inject]
        private BlockProvider BlockDataProvider { get; }

        [Inject]
        private MaterialProvider MaterialManager { get; }

        private Chunk[] chunks;
        private Chunk[] chunksBuffer;
        private ChunkRenderer[] renderers;
        private ChunkRenderer[] renderersBuffer;

        private int RendererToIndex(Vector3Int coordinate) {
            return Array3DUtility.To1D(coordinate.x - Center.x + DrawDistance,
                coordinate.y, coordinate.z - Center.y + DrawDistance, RenderersSize, HEIGHT);
        }

        private int ChunkToIndex(Vector3Int coordinate) {
			return Array3DUtility.To1D(coordinate.x - Center.x + DrawDistance + 1,
                coordinate.y, coordinate.z - Center.y + DrawDistance + 1, ChunksSize, HEIGHT);
        }

        private ConcurrentStack<ChunkRenderer> renderersToDestroy = new();

        private void UpdateCenter(Vector2Int center) {
            Array.Fill(chunksBuffer, null);
            Array.Fill(renderersBuffer, null);

            var d = center - this.center;
            for (int x = 0; x < ChunksSize; x++) {
                for (int z = 0; z < ChunksSize; z++) {
                    for (int y = 0; y < HEIGHT; y++) {
                        var index = Array3DUtility.To1D(x, y, z, ChunksSize, HEIGHT);
						var chunk = chunks[index];
                        if (chunk == null)
                            continue;
                        int nx = x - d.x;
                        int nz = z - d.y;
                        if (nx < 0 || nz < 0 || nx >= ChunksSize || nz >= ChunksSize) {
                            chunks[index] = null;
                            continue;
                        }
                        chunksBuffer[Array3DUtility.To1D(nx, y, nz, ChunksSize, HEIGHT)] = chunk;
                    }
                }
            }

            for (int x = 0; x < RenderersSize; x++) {
                for (int z = 0; z < RenderersSize; z++) {
                    for (int y = 0; y < HEIGHT; y++) {
						var rindex = Array3DUtility.To1D(x, y, z, RenderersSize, HEIGHT);
						var renderer = renderers[rindex];
						if (renderer == null)
							continue;
						int nx = x - d.x;
						int nz = z - d.y;
						if (nx < 0 || nz < 0 || nx >= RenderersSize || nz >= RenderersSize) {
							renderersToDestroy.Push(renderer);
							renderers[rindex] = null;
							continue;
						}
						renderersBuffer[Array3DUtility.To1D(nx, y, nz, RenderersSize, HEIGHT)] = renderer;
					}
                }
            }

			(chunks, chunksBuffer) = (chunksBuffer, chunks);
            (renderers, renderersBuffer) = (renderersBuffer, renderers);

            this.center = center;
        }

        public IEnumerator CleanRenderers() {
            while (true) {
                while (renderersToDestroy.TryPop(out ChunkRenderer chunkRenderer)) {
                    Destroy(chunkRenderer.gameObject);
                    yield return null;
                }

                yield return null;
            }
        }

        public bool HasChunk(Vector3Int coordinate) {
			if (coordinate.y == -1 || coordinate.y == HEIGHT * Chunk.SIZE)
				return false;
			int index = ChunkToIndex(coordinate);
            return index >= 0 && index < ChunksVolume && chunks[index] != null;
		}

        public bool HasRenderer(Vector3Int coordinate) {
			if (coordinate.y == -1 || coordinate.y == HEIGHT * Chunk.SIZE)
				return false;
			int index = RendererToIndex(coordinate);
            return index >= 0 && index < RenderersVolume && renderers[index] != null;
		}

        public Chunk CreateChunk(Vector3Int coordinate) {
			if (coordinate.y == -1 || coordinate.y == HEIGHT * Chunk.SIZE)
				throw new Exception("Coordinate is out of range");
			var chunk = new Chunk();
            chunk.Coordinate = coordinate;
            chunks[ChunkToIndex(coordinate)] = chunk;
            return chunk;
        }

        public Chunk GetChunk(Vector3Int coordinate) {
            if (coordinate.y == -1 || coordinate.y == HEIGHT * Chunk.SIZE)
                return null;
			int index = ChunkToIndex(coordinate);
			if (index >= 0 && index < ChunksVolume)
				return chunks[index];
            return null;
		}

		public void SetChunk(Vector3Int coordinate, Chunk chunk) {
			if (coordinate.y == -1 || coordinate.y == HEIGHT * Chunk.SIZE)
				throw new Exception("Coordinate is out of range");
			chunks[ChunkToIndex(coordinate)] = chunk;
		}

        public ChunkRenderer CreateRenderer(Vector3Int coordinate) {
			if (coordinate.y == -1 || coordinate.y == HEIGHT * Chunk.SIZE)
				throw new Exception("Coordinate is out of range");
			var renderer = Instantiate(this.renderer, transform);
            var chunk = GetOrCreateChunk(coordinate);
            renderer.Initialize(chunk);
            renderers[RendererToIndex(coordinate)] = renderer;
            return renderer;
        }

		public bool TryGetChunk(Vector3Int coordinate, out Chunk chunk) {
			if (coordinate.y == -1 || coordinate.y == HEIGHT * Chunk.SIZE) {
                chunk = null;
                return false;
            }

			int index = ChunkToIndex(coordinate);
			if (index >= 0 && index < ChunksVolume) {
                chunk = chunks[index];
                return chunk != null;
            }

            chunk = null;
            return false;
		}

		public bool TryGetRenderer(Vector3Int coordinate, out ChunkRenderer renderer) {
			if (coordinate.y == -1 || coordinate.y == HEIGHT * Chunk.SIZE) {
				renderer = null;
				return false;
			}

			int index = RendererToIndex(coordinate);
			if (index >= 0 && index < RenderersVolume) {
				renderer = renderers[index];
				return renderer != null;
			}

			renderer = null;
			return false;
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
			if (coordinate.y == -1 || coordinate.y == HEIGHT * Chunk.SIZE)
				throw new Exception("Coordinate is out of range");
			chunks[ChunkToIndex(coordinate)] = null;
        }

        public void DestroyRenderer(Vector3Int coordinate) {
			if (coordinate.y == -1 || coordinate.y == HEIGHT * Chunk.SIZE)
				throw new Exception("Coordinate is out of range");
			var index = RendererToIndex(coordinate);
            Destroy(renderers[index]);
            renderers[index] = null;
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
            chunks = new Chunk[ChunksVolume];
            chunksBuffer = new Chunk[ChunksVolume];
            renderers = new ChunkRenderer[RenderersVolume];
            renderersBuffer = new ChunkRenderer[RenderersVolume];

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
                if (renderer == null)
                    continue;
                if (renderer.Data.IsComplete && renderer.Data.IsDirty)
                    renderer.UpdateMesh(ChunkUtility.GenerateMeshData(this, renderer.Data, BlockDataProvider), MaterialManager);
            }
        }

		private void OnDrawGizmos() {
			if (debugChunks) {
                foreach (var chunk in chunks) {
                    if (chunk == null)
                        continue;
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(chunk.Coordinate * Chunk.SIZE + Vector3.one * Chunk.SIZE / 2.0f, 
                        Vector3.one * Chunk.SIZE - Vector3.one * 0.01f);
                }
            }
		}
	}
}