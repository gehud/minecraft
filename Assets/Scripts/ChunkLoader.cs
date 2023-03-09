using Minecraft.Utilities;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

namespace Minecraft {
    public class ChunkLoader : MonoBehaviour {
        [SerializeField]
        private UnityEvent onWorldCreate;

        [SerializeField]
        private Transform player;
        [SerializeField]
        private float loadCountdown = 0.5f;

        private Vector2Int center;
        private readonly ConcurrentStack<Vector3Int> chunks = new();
        private readonly ConcurrentStack<Vector3Int> renderers = new();
        private readonly ConcurrentStack<Vector2Int> sunlights = new();

        bool worldGenerated = false;

        [Inject]
        private readonly World world;

        [Inject]
        private readonly BlockProvider blockDataProvider;

        [Inject]
        private readonly MaterialProvider materialManager;

        [Inject]
        private readonly ChunkGenerator chunkGenerator;

        private readonly CancellationTokenSource cancellationTokenSource = new();

        private Task lastLoading;
        private bool isLoadingCanceled = false;

        private Vector3Int GetPlayerChunk() {
            Vector3Int coordinate = CoordinateUtility.ToCoordinate(player.position);
            return CoordinateUtility.ToChunk(coordinate);
        }

        private void UpdateLoadData(Vector2Int column) {            
            chunks.Clear();
            renderers.Clear();
            sunlights.Clear();
			int startX = column.x - 1;
			int endX = column.x + 1;
			int startZ = column.y - 1;
			int endZ = column.y + 1;
			for (int x = startX; x <= endX; x++) {
                for (int z = startZ; z <= endZ; z++) {
					bool sunlight = false;
                    for (int y = 0; y < World.HEIGHT; y++) {
                        Vector3Int chunkCoordinate = new Vector3Int(x, y, z);
                        if (x != startX && x != endX && z != startZ && z != endZ) {
                            if (!world.HasRenderer(chunkCoordinate)) {
                                renderers.Push(chunkCoordinate);
                            }
                        }

                        if (!world.HasChunk(chunkCoordinate)) {
                            chunks.Push(chunkCoordinate);
                            sunlight = true;
                        }
                    }

                    if (sunlight) {
                        sunlights.Push(new Vector2Int(x, z));
                    }

					if (isLoadingCanceled)
						return;
				}
            }
		}

        private async Task LoadChunks() {
			world.Center = center;

            for (int zone = 1; zone <= world.DrawDistance; zone++) {
                if (isLoadingCanceled)
                    return;

				int startX = world.Center.x - zone;
				int endX = world.Center.x + zone;
				int startZ = world.Center.y - zone;
				int endZ = world.Center.y + zone;
                for (int x = startX; x <= endX; x++) {
                    for (int z = startZ; z <= endZ; z++) {
						ConcurrentDictionary<Vector3Int, Chunk> generatedData = new();
						ConcurrentDictionary<Vector3Int, ConcurrentDictionary<MaterialType, MeshData>> generatedMeshDatas = new();
						if (isLoadingCanceled)
							return;
						await Task.Run(() => {
							UpdateLoadData(new Vector2Int(x, z));

							foreach (var item in chunks)
								generatedData.TryAdd(item, chunkGenerator.Generate(item));

							foreach (var item in generatedData) {
								foreach (var treeRoot in item.Value.TreeData.Positions) {
									GenerateTree(CoordinateUtility.ToGlobal(item.Value.Coordinate, treeRoot));
								}
							}

							foreach (var item in renderers) {
								ChunkUtility.ParallelFor((localBlockCoordinate) => {
									Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(item, localBlockCoordinate);
									if (world.GetBlock(blockCoordinate) == BlockType.Water) {
                                        if (LiquidCalculator.ShouldFlow(world, blockCoordinate))
                                            world.LiquidCalculatorWater.Add(blockCoordinate);
                                    }
								});
							}

							foreach (var item in sunlights)
								LightCalculator.AddSunlight(world, item);

							world.LightCalculatorSun.Calculate();

							foreach (var item in renderers)
								generatedMeshDatas.TryAdd(item, ChunkUtility.GenerateMeshData(world, world.GetChunk(item), blockDataProvider));
						}, cancellationTokenSource.Token);

						foreach (var item in generatedMeshDatas) {
							ChunkRenderer renderer = world.CreateRenderer(item.Key);
							renderer.UpdateMesh(item.Value, materialManager);
							await Task.Yield();
						}
					}
                }

				worldGenerated = true;
			}
        }

        private void GenerateTree(Vector3Int blockCoordinate) {
            for (int i = 0; i < 6; i++) {
                Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                if (world.TryGetChunk(chunkCoordinate, out Chunk chunk)) {
                    Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                    chunk.BlockMap[localBlockCoordinate] = BlockType.Log;
                }

                blockCoordinate += Vector3Int.up;
            }

            void PlaceLeaves(int x, int y, int z) {
				Vector3Int leavesCoordinate = blockCoordinate + new Vector3Int(x, y, z) + Vector3Int.down;
				Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(leavesCoordinate);
				if (world.TryGetChunk(chunkCoordinate, out Chunk chunk)) {
					Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, leavesCoordinate);
					if (!blockDataProvider.Get(chunk.BlockMap[localBlockCoordinate]).IsSolid)
						chunk.BlockMap[localBlockCoordinate] = BlockType.Leaves;
				}
			}

            for (int x = -2; x <= 2; x++) {
                for (int y = -2; y < 2; y++) {
                    for (int z = -2; z <= 2; z++) {
                        if (y >= 0) {
                            if (x != -2 && x != 2 && z != -2 && z != 2) {
                                if (y == 0) {
                                    PlaceLeaves(x, y, z);
                                } else if (!(x == -1 && z == -1 || x == -1 && z == 1 || x == 1 && z == -1 || x == 1 && z == 1)) {
                                    PlaceLeaves(x, y, z);
                                }
                            }
                        } else if (!(x == -2 && z == -2 || x == -2 && z == 2 || x == 2 && z == -2 || x == 2 && z == 2)) {
                            PlaceLeaves(x, y, z);
                        }
					}
                }
            }
        }

        private IEnumerator CheckLoadRequirement() {
            while (true) {
                Vector3Int playerChunk = GetPlayerChunk();
                if (playerChunk.x != center.x || playerChunk.z != center.y) {
                    center = new Vector2Int(playerChunk.x, playerChunk.z);
                    if (lastLoading != null && !lastLoading.IsCompleted) {
                        isLoadingCanceled = true;
				    }

                    yield return new WaitUntil(() => lastLoading.IsCompleted);

                    isLoadingCanceled = false;
                    lastLoading = LoadChunks();
                }

                yield return new WaitForSeconds(loadCountdown);
            }
        }

        private IEnumerator Start() {
			var playerChunk = GetPlayerChunk();
			center = new Vector2Int(playerChunk.x, playerChunk.z);
			lastLoading = LoadChunks();
			StartCoroutine(CheckLoadRequirement());
			yield return new WaitUntil(() => worldGenerated);
			onWorldCreate?.Invoke();
		}

		private void OnDisable() {
			cancellationTokenSource.Cancel();
		}
	}
}