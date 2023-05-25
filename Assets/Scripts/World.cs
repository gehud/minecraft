using Minecraft.Utilities;
using System;
using System.Collections;
using UnityEngine;
using Zenject;

namespace Minecraft {
	public class World : MonoBehaviour {
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

        public LiquidCalculator LiquidCalculatorWater { get; set; }

        [SerializeField]
        private Material sMaterial;
		[SerializeField]
		private Material tMaterial;
		[SerializeField]
        private bool debugChunks = false;

        [SerializeField, Min(0)]
        private float tick = 0.25f;

        [Inject]
        private readonly BlockProvider blockProvider;

        [Inject]
        private readonly SaveManager saveManager;

        [Inject]
        private readonly LightSolver lightSolver;

		private int chunksSize;
		private int chunksVolume;
        private Chunk[] chunks;
        private Chunk[] chunksBuffer;
		private int renderersSize;
		private int renderersVolume;
        private ChunkRenderer[] renderers;
        private ChunkRenderer[] renderersBuffer;

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
            var chunk = GetChunk(coordinate);
            var renderer = new ChunkRenderer(chunk);
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

        public int GetLightLevel(Vector3Int blockCoordinate, int chanel) {
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

			lightSolver.RemoveLight(LightMap.RED, blockCoordinate);
			lightSolver.RemoveLight(LightMap.GREEN, blockCoordinate);
			lightSolver.RemoveLight(LightMap.BLUE, blockCoordinate);
			lightSolver.Solve(LightMap.RED);
			lightSolver.Solve(LightMap.GREEN);
			lightSolver.Solve(LightMap.BLUE);

            if (GetBlock(blockCoordinate + Vector3Int.up) == BlockType.Air
                && GetLightLevel(blockCoordinate + Vector3Int.up, LightMap.SUN) == LightMap.MAX) {
                for (int y = blockCoordinate.y; y >= 0; y--) {
                    if (GetBlock(new Vector3Int(blockCoordinate.x, y, blockCoordinate.z)) != BlockType.Air)
                        break;
					lightSolver.AddLight(LightMap.SUN, blockCoordinate.x, y, blockCoordinate.z, LightMap.MAX);
                }
            }

            LiquidCalculatorWater.Add(blockCoordinate + Vector3Int.right);
            LiquidCalculatorWater.Add(blockCoordinate + Vector3Int.left);
            LiquidCalculatorWater.Add(blockCoordinate + Vector3Int.up);
            LiquidCalculatorWater.Add(blockCoordinate + Vector3Int.forward);
            LiquidCalculatorWater.Add(blockCoordinate + Vector3Int.back);

            lightSolver.AddLight(LightMap.RED, blockCoordinate + Vector3Int.right);
            lightSolver.AddLight(LightMap.RED, blockCoordinate + Vector3Int.left);
            lightSolver.AddLight(LightMap.RED, blockCoordinate + Vector3Int.up);
            lightSolver.AddLight(LightMap.RED, blockCoordinate + Vector3Int.down);
            lightSolver.AddLight(LightMap.RED, blockCoordinate + Vector3Int.forward);
            lightSolver.AddLight(LightMap.RED, blockCoordinate + Vector3Int.back);
			lightSolver.Solve(LightMap.RED);

            lightSolver.AddLight(LightMap.GREEN, blockCoordinate + Vector3Int.right);
            lightSolver.AddLight(LightMap.GREEN, blockCoordinate + Vector3Int.left);
            lightSolver.AddLight(LightMap.GREEN, blockCoordinate + Vector3Int.up);
            lightSolver.AddLight(LightMap.GREEN, blockCoordinate + Vector3Int.down);
            lightSolver.AddLight(LightMap.GREEN, blockCoordinate + Vector3Int.forward);
            lightSolver.AddLight(LightMap.GREEN, blockCoordinate + Vector3Int.back);
			lightSolver.Solve(LightMap.GREEN);

            lightSolver.AddLight(LightMap.BLUE, blockCoordinate + Vector3Int.right);
            lightSolver.AddLight(LightMap.BLUE, blockCoordinate + Vector3Int.left);
            lightSolver.AddLight(LightMap.BLUE, blockCoordinate + Vector3Int.up);
            lightSolver.AddLight(LightMap.BLUE, blockCoordinate + Vector3Int.down);
            lightSolver.AddLight(LightMap.BLUE, blockCoordinate + Vector3Int.forward);
            lightSolver.AddLight(LightMap.BLUE, blockCoordinate + Vector3Int.back);
			lightSolver.Solve(LightMap.BLUE);

            lightSolver.AddLight(LightMap.SUN, blockCoordinate + Vector3Int.right);
            lightSolver.AddLight(LightMap.SUN, blockCoordinate + Vector3Int.left);
            lightSolver.AddLight(LightMap.SUN, blockCoordinate + Vector3Int.up);
            lightSolver.AddLight(LightMap.SUN, blockCoordinate + Vector3Int.down);
            lightSolver.AddLight(LightMap.SUN, blockCoordinate + Vector3Int.forward);
            lightSolver.AddLight(LightMap.SUN, blockCoordinate + Vector3Int.back);
			lightSolver.Solve(LightMap.SUN);
        }

        public void PlaceVoxel(Vector3Int blockCoordinate, BlockType voxelType) {
            LiquidCalculatorWater.Remove(blockCoordinate);

            SetBlock(blockCoordinate, voxelType);

			lightSolver.RemoveLight(LightMap.RED, blockCoordinate);
			lightSolver.RemoveLight(LightMap.GREEN, blockCoordinate);
			lightSolver.RemoveLight(LightMap.BLUE, blockCoordinate);
			lightSolver.RemoveLight(LightMap.SUN, blockCoordinate);
            for (int y = blockCoordinate.y - 1; y >= 0; y--) {
                if (!blockProvider.Get(GetBlock(new Vector3Int(blockCoordinate.x, y, blockCoordinate.z))).IsTransparent)
                    break;
				lightSolver.RemoveLight(LightMap.SUN, blockCoordinate.x, y, blockCoordinate.z);
            }
            lightSolver.Solve(LightMap.RED);
            lightSolver.Solve(LightMap.GREEN);
            lightSolver.Solve(LightMap.BLUE);
            lightSolver.Solve(LightMap.SUN);

			LightColor emission = blockProvider.Get(voxelType).Emission;
            if (emission.R != 0) {
				lightSolver.AddLight(LightMap.RED, blockCoordinate, emission.R);
				lightSolver.Solve(LightMap.RED);
            }
            if (emission.G != 0) {
				lightSolver.AddLight(LightMap.GREEN, blockCoordinate, emission.G);
				lightSolver.Solve(LightMap.GREEN);
            }
            if (emission.B != 0) {
				lightSolver.AddLight(LightMap.BLUE, blockCoordinate, emission.B);
				lightSolver.Solve(LightMap.BLUE);
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
                        if (nx < 0 || nz < 0 || nx >= renderersSize || nz >= renderersSize)
                            continue;
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
			LiquidCalculator.SetBlockDataManager(blockProvider);
			LiquidCalculatorWater = new LiquidCalculator(this, BlockType.Water);
		}

		private void Start() {
            StartCoroutine(SolveLiquid());
		}

		private void Update() {
            foreach (var renderer in renderers) {
                if (renderer == null)
                    continue;
                if (renderer.Data.IsDirty) {
                    renderer.UpdateMesh(new ChunkRendererDataJob(this, renderer.Data, blockProvider));
                    if (renderer.Data.IsModified) {
                        saveManager.SaveChunk(renderer.Data);
                    }
                }
                renderer.Render(sMaterial, tMaterial);
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