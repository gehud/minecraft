using Minecraft.Utilities;
using System;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Minecraft {
	public class World : NetworkBehaviour {
        public static World Instance => instance;
        private static World instance;
        /// <summary>
        /// World height in Chunks.
        /// </summary>
        public const int HEIGHT = 16;

        /// <summary>
        /// Center of world rendering.
        /// </summary>
        public Vector2Int Center {
            get => center;
            set {
                UpdateCenter(value);
            }
        }

        public const int MIN_DRAW_DISTANCE = 2;
        public const int MAX_DRAW_DISTANCE = 32;

		/// <summary>
		/// Radius in chunks of rendering area.
		/// </summary>
		public int DrawDistance { 
            get => drawDistance;
            set {
                drawDistance = Mathf.Clamp(value, MIN_DRAW_DISTANCE, MAX_DRAW_DISTANCE);
                UpdateMetrics();
                Center = Center;
                OnDrawDistanceChanged?.Invoke();
            }
        }

        private int drawDistance = 2;

        public event Action OnDrawDistanceChanged;

        private Vector2Int center = Vector2Int.zero;

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
        private readonly BlockProvider blockDataProvider;
         
        [Inject]
        private readonly MaterialProvider materialProvider;

        [Inject]
        private readonly SaveManager saveManager;

		private int chunksSize;
		private int chunksVolume;
        private Chunk[] chunks;
        private Chunk[] chunksBuffer;
		private int renderersSize;
		private int renderersVolume;
        private ChunkRenderer[] renderers;
        private ChunkRenderer[] renderersBuffer;
		private readonly ConcurrentQueue<ChunkRenderer> renderersToDestroy = new();

		public bool HasChunk(Vector3Int coordinate) {
			var arrayCoordinate = new Vector3Int(coordinate.x - Center.x + DrawDistance + 1,
				coordinate.y, coordinate.z - Center.y + DrawDistance + 1);
            if (arrayCoordinate.x < 0 || arrayCoordinate.x >= chunksSize
                || arrayCoordinate.y < 0 || arrayCoordinate.y >= HEIGHT
                || arrayCoordinate.z < 0 || arrayCoordinate.z >= chunksSize)
                return false;
			return chunks[ChunkToIndex(coordinate)] != null;
		}

		public bool HasRenderer(Vector3Int coordinate) {
			var arrayCoordinate = new Vector3Int(coordinate.x - Center.x + DrawDistance,
				coordinate.y, coordinate.z - Center.y + DrawDistance);
            if (arrayCoordinate.x < 0 || arrayCoordinate.x >= renderersSize
                || arrayCoordinate.y < 0 || arrayCoordinate.y >= HEIGHT
                || arrayCoordinate.z < 0 || arrayCoordinate.z >= renderersSize)
                return false;
			return renderers[RendererToIndex(coordinate)] != null;
		}

        public void SetChunk(Vector3Int coordinate, Chunk chunk) {
			chunks[ChunkToIndex(coordinate)] = chunk;
		}

        public Chunk GetChunk(Vector3Int coordinate) {
            if (!HasChunk(coordinate))
                return null;
			return chunks[ChunkToIndex(coordinate)];
		}

        public ChunkRenderer CreateRenderer(Vector3Int coordinate) {
			if (HasRenderer(coordinate))
				throw new Exception("Renderer allready exists.");
            if (!HasChunk(coordinate))
                throw new Exception("Renderer requires chunk.");
			var renderer = Instantiate(this.renderer);
            var chunk = GetChunk(coordinate);
            renderer.Initialize(chunk);
            renderers[RendererToIndex(coordinate)] = renderer;
            return renderer;
        }

		public bool TryGetChunk(Vector3Int coordinate, out Chunk chunk) {
            chunk = GetChunk(coordinate);
            return chunk != null;
		}

		public ChunkRenderer GetRenderer(Vector3Int coordinate) {
			if (!HasRenderer(coordinate))
				return null;
			return renderers[RendererToIndex(coordinate)];
		}

		public bool TryGetRenderer(Vector3Int coordinate, out ChunkRenderer renderer) {
            renderer = GetRenderer(coordinate);
			return renderer != null;
		}

        public ChunkRenderer GetOrCreateRenderer(Vector3Int coordinate) {
            if (TryGetRenderer(coordinate, out ChunkRenderer renderer))
                return renderer;
            return CreateRenderer(coordinate);
        }

		public void MarkDirtyIfNeeded(Vector3Int chunkCoordinate, Vector3Int localBlockCoordinate) {
            if (localBlockCoordinate.x == 0 && TryGetChunk(chunkCoordinate + Vector3Int.left, out var chunk))
                chunk.IsDirty = true;
			if (localBlockCoordinate.y == 0 && TryGetChunk(chunkCoordinate + Vector3Int.down, out chunk))
				chunk.IsDirty = true;
			if (localBlockCoordinate.z == 0 && TryGetChunk(chunkCoordinate + Vector3Int.back, out chunk))
				chunk.IsDirty = true; 
			if (localBlockCoordinate.x == Chunk.SIZE - 1 && TryGetChunk(chunkCoordinate + Vector3Int.right, out chunk))
				chunk.IsDirty = true;
			if (localBlockCoordinate.y == Chunk.SIZE - 1 && TryGetChunk(chunkCoordinate + Vector3Int.up, out chunk))
				chunk.IsDirty = true;
			if (localBlockCoordinate.z == Chunk.SIZE - 1 && TryGetChunk(chunkCoordinate + Vector3Int.forward, out chunk))
				chunk.IsDirty = true;
		}

		public void MarkModifiedIfNeeded(Vector3Int chunkCoordinate, Vector3Int localBlockCoordinate) {
			if (localBlockCoordinate.x == 0 && TryGetChunk(chunkCoordinate + Vector3Int.left, out var chunk))
				chunk.IsModified = true;
			if (localBlockCoordinate.y == 0 && TryGetChunk(chunkCoordinate + Vector3Int.down, out chunk))
				chunk.IsModified = true;
			if (localBlockCoordinate.z == 0 && TryGetChunk(chunkCoordinate + Vector3Int.back, out chunk)) 
                chunk.IsModified = true;
			if (localBlockCoordinate.x == Chunk.SIZE - 1 && TryGetChunk(chunkCoordinate + Vector3Int.right, out chunk)) 
                chunk.IsModified = true;
			if (localBlockCoordinate.y == Chunk.SIZE - 1 && TryGetChunk(chunkCoordinate + Vector3Int.up, out chunk)) 
                chunk.IsModified = true;
			if (localBlockCoordinate.z == Chunk.SIZE - 1 && TryGetChunk(chunkCoordinate + Vector3Int.forward, out chunk))
				chunk.IsModified = true;
		}

		public void MarkDirtyAndModifiedIfNeeded(Vector3Int chunkCoordinate, Vector3Int localBlockCoordinate) {
			if (localBlockCoordinate.x == 0 && TryGetChunk(chunkCoordinate + Vector3Int.left, out var chunk)) {
                chunk.IsDirty = true;
				chunk.IsModified = true;
			}
			if (localBlockCoordinate.y == 0 && TryGetChunk(chunkCoordinate + Vector3Int.down, out chunk)) {
				chunk.IsDirty = true;
				chunk.IsModified = true;
			}
			if (localBlockCoordinate.z == 0 && TryGetChunk(chunkCoordinate + Vector3Int.back, out chunk)) {
				chunk.IsDirty = true;
				chunk.IsModified = true;
			}
			if (localBlockCoordinate.x == Chunk.SIZE - 1 && TryGetChunk(chunkCoordinate + Vector3Int.right, out chunk)) {
				chunk.IsDirty = true;
				chunk.IsModified = true;
			}
			if (localBlockCoordinate.y == Chunk.SIZE - 1 && TryGetChunk(chunkCoordinate + Vector3Int.up, out chunk)) {
				chunk.IsDirty = true;
				chunk.IsModified = true;
			}
			if (localBlockCoordinate.z == Chunk.SIZE - 1 && TryGetChunk(chunkCoordinate + Vector3Int.forward, out chunk)) {
				chunk.IsDirty = true;
				chunk.IsModified = true;
			}
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
                chunk.IsDirty = true;
                chunk.IsModified = true;
				MarkDirtyAndModifiedIfNeeded(chunkCoordinate, localBlockCoordinate);
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
                return chunk.LiquidMap[localBlockCoordinate];
            }

            return LiquidMap.MIN;
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
                if (!blockDataProvider.Get(GetBlock(new Vector3Int(blockCoordinate.x, y, blockCoordinate.z))).IsTransparent)
                    break;
                LightCalculatorSun.Remove(blockCoordinate.x, y, blockCoordinate.z);
            }
            LightCalculatorRed.Calculate();
            LightCalculatorGreen.Calculate();
            LightCalculatorBlue.Calculate();
            LightCalculatorSun.Calculate();

            LightColor emission = blockDataProvider.Get(voxelType).Emission;
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

		private void UpdateMetrics() {
            var oldChunksSize = chunksSize;
            var oldRenderersSize = renderersSize;
            var oldChunks = chunks;
            var oldRenderers = renderers;
			chunksSize = DrawDistance * 2 + 3;
			renderersSize = DrawDistance * 2 + 1;
			chunksVolume = chunksSize * chunksSize * HEIGHT;
			renderersVolume = renderersSize * renderersSize * HEIGHT;
			chunks = new Chunk[chunksVolume];
			chunksBuffer = new Chunk[chunksVolume];
			renderers = new ChunkRenderer[renderersVolume];
            renderersBuffer = new ChunkRenderer[renderersVolume];

            var d = chunksSize - oldChunksSize;
            for (int x = 0; x < oldChunksSize; x++) {
                for (int z = 0; z < oldChunksSize; z++) {
                    for (int y = 0; y < HEIGHT; y++) {
                        var index = Array3DUtility.To1D(x, y, z, oldChunksSize, HEIGHT);
                        var chunk = oldChunks[index];
                        if (chunk == null)
                            continue;
                        int nx = x + d / 2;
                        int nz = z + d / 2;
                        if (nx < 0 || nz < 0 || nx >= chunksSize || nz >= chunksSize)
                            continue;
                        chunks[Array3DUtility.To1D(nx, y, nz, chunksSize, HEIGHT)] = chunk;
                    }
                }
            }

            d = renderersSize - oldRenderersSize;
            for (int x = 0; x < oldRenderersSize; x++) {
                for (int z = 0; z < oldRenderersSize; z++) {
                    for (int y = 0; y < HEIGHT; y++) {
                        var index = Array3DUtility.To1D(x, y, z, oldRenderersSize, HEIGHT);
                        var renderer = oldRenderers[index];
                        if (renderer == null)
                            continue;
                        int nx = x + d / 2;
                        int nz = z + d / 2;
                        if (nx < 0 || nz < 0 || nx >= renderersSize || nz >= renderersSize) {
                            Destroy(renderer.gameObject);
                            continue;
                        }
                        renderers[Array3DUtility.To1D(nx, y, nz, renderersSize, HEIGHT)] = renderer;
                    }
                }
            }
        }

        private int RendererToIndex(Vector3Int coordinate) {
			var arrayCoordinate = new Vector3Int(coordinate.x - Center.x + DrawDistance,
				coordinate.y, coordinate.z - Center.y + DrawDistance);
			return Array3DUtility.To1D(arrayCoordinate.x, arrayCoordinate.y, arrayCoordinate.z, renderersSize, HEIGHT);
		}

		private int ChunkToIndex(Vector3Int coordinate) {
			var arrayCoordinate = new Vector3Int(coordinate.x - Center.x + DrawDistance + 1,
				coordinate.y, coordinate.z - Center.y + DrawDistance + 1);
			return Array3DUtility.To1D(arrayCoordinate.x, arrayCoordinate.y, arrayCoordinate.z, chunksSize, HEIGHT);
		}

		private void UpdateCenter(Vector2Int center) {
			Array.Fill(chunksBuffer, null);
			Array.Fill(renderersBuffer, null);

			var d = center - this.center;
			for (int x = 0; x < chunksSize; x++) {
				for (int z = 0; z < chunksSize; z++) {
					for (int y = 0; y < HEIGHT; y++) {
						var index = Array3DUtility.To1D(x, y, z, chunksSize, HEIGHT);
						var chunk = chunks[index];
						if (chunk == null)
							continue;
						int nx = x - d.x;
						int nz = z - d.y;
						if (nx < 0 || nz < 0 || nx >= chunksSize || nz >= chunksSize) {
							chunks[index] = null;
							continue;
						}
						chunksBuffer[Array3DUtility.To1D(nx, y, nz, chunksSize, HEIGHT)] = chunk;
					}
				}
			}

            for (int x = 0; x < renderersSize; x++) {
                for (int z = 0; z < renderersSize; z++) {
                    for (int y = 0; y < HEIGHT; y++) {
                        var index = Array3DUtility.To1D(x, y, z, renderersSize, HEIGHT);
                        var renderer = renderers[index];
                        if (renderer == null)
                            continue;
                        int nx = x - d.x;
                        int nz = z - d.y;
                        if (nx < 0 || nz < 0 || nx >= renderersSize || nz >= renderersSize) {
                            renderersToDestroy.Enqueue(renderer);
                            renderers[index] = null;
                            continue;
                        }
                        renderersBuffer[Array3DUtility.To1D(nx, y, nz, renderersSize, HEIGHT)] = renderer;
                    }
				}
            }

			(chunks, chunksBuffer) = (chunksBuffer, chunks);
			(renderers, renderersBuffer) = (renderersBuffer, renderers);

			this.center = center;
		}

		private IEnumerator SolveLiquid() {
			while (true) {
				LiquidCalculatorWater.Calculate();
				yield return new WaitForSeconds(tick);
			}
		}

		private IEnumerator CleanRenderers() {
			while (true) {
				while (renderersToDestroy.TryDequeue(out ChunkRenderer chunkRenderer)) {
					Destroy(chunkRenderer.gameObject);
					yield return null;
				}

				yield return null;
			}
		}

		private void Awake() {
            instance = this;
			chunksSize = DrawDistance * 2 + 3;
			renderersSize = DrawDistance * 2 + 1;
			chunksVolume = chunksSize * chunksSize * HEIGHT;
			renderersVolume = renderersSize * renderersSize * HEIGHT;
			chunks = new Chunk[chunksVolume];
			chunksBuffer = new Chunk[chunksVolume];
			renderers = new ChunkRenderer[renderersVolume];
			renderersBuffer = new ChunkRenderer[renderersVolume];

			LightCalculator.SetBlockDataManager(blockDataProvider);
			LightCalculatorRed = new LightCalculator(this, LightChanel.Red);
			LightCalculatorGreen = new LightCalculator(this, LightChanel.Green);
			LightCalculatorBlue = new LightCalculator(this, LightChanel.Blue);
			LightCalculatorSun = new LightCalculator(this, LightChanel.Sun);
			LiquidCalculator.SetBlockDataManager(blockDataProvider);
			LiquidCalculatorWater = new LiquidCalculator(this, BlockType.Water);
		}

		private void Start() {
            StartCoroutine(SolveLiquid());
            StartCoroutine(CleanRenderers());
		}

		private void Update() {
            foreach (var renderer in renderers) {
                if (renderer == null)
                    continue;
                if (renderer.Data.IsDirty) { 
                    renderer.UpdateMesh(ChunkUtility.GenerateMeshData(this, renderer.Data, blockDataProvider), materialProvider);
                    if (renderer.Data.IsModified) {
                        saveManager.SaveChunk(renderer.Data);
                    }
                }
            }
        }

        private void DrawChunkGizmo(Vector3Int coordinate) {
            var center = coordinate * Chunk.SIZE + Vector3.one * Chunk.SIZE / 2.0f;
            var size = Vector3.one * Chunk.SIZE - Vector3.one * 0.01f;
			Gizmos.DrawWireCube(center, size);
		}

		private void OnDrawGizmos() {
			if (!debugChunks) {
				return;
			}

			foreach (var chunk in chunks) {
				if (chunk == null)
					continue;
				Gizmos.color = Color.green;
				DrawChunkGizmo(chunk.Coordinate);
			}
		}
	}
}