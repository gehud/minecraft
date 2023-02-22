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
        public UnityEvent OnWorldCreate;

        [SerializeField]
        private Transform player;
        [SerializeField]
        private float loadCountdown = 0.5f;

        private Vector3Int lastPlayerChunk;
        private readonly ConcurrentStack<Vector3Int> chunkToCreateCoordinates = new();
        private readonly ConcurrentStack<Vector3Int> rendererToCreateCoordinates = new();
        private readonly ConcurrentStack<Vector2Int> sunlightCoordinatesToCalculate = new();

        bool generateChunks = false;
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

        private Vector3Int GetPlayerChunk() {
            Vector3Int coordinate = CoordinateUtility.ToCoordinate(player.position);
            return CoordinateUtility.ToChunk(coordinate);
        }

        private void GenerateLoadData() {
            chunkToCreateCoordinates.Clear();
            rendererToCreateCoordinates.Clear();
            sunlightCoordinatesToCalculate.Clear();
            world.Center = new Vector2Int(lastPlayerChunk.x, lastPlayerChunk.z);
            int startX = lastPlayerChunk.x - world.DrawDistance - 1;
            int endX = lastPlayerChunk.x + world.DrawDistance + 1;
            int startZ = lastPlayerChunk.z - world.DrawDistance - 1;
            int endZ = lastPlayerChunk.z + world.DrawDistance + 1;
            for (int x = startX; x <= endX; x++)
                for (int z = startZ; z <= endZ; z++) {
                    bool sunlight = false;
                    for (int y = 0; y < World.HEIGHT; y++) {
                        Vector3Int chunkCoordinate = new Vector3Int(x, y, z);
                        if (x != startX && x != endX && z != startZ && z != endZ) {
                            if (!world.HasRenderer(chunkCoordinate))
                                rendererToCreateCoordinates.Push(chunkCoordinate);
                        }

                        if (!world.HasChunk(chunkCoordinate)) {
                            chunkToCreateCoordinates.Push(chunkCoordinate);
                            sunlight = true;
                        }
                    }

                    if (sunlight) {
                        sunlightCoordinatesToCalculate.Push(new Vector2Int(x, z));
                    }
                }
		}

        private IEnumerator GenerateChunks(World world, ConcurrentDictionary<Vector3Int, ConcurrentDictionary<MaterialType, MeshData>> meshDatas) {
            generateChunks = true;

			foreach (var item in meshDatas) {
                ChunkRenderer chunk = world.GetOrCreateRenderer(item.Key);
                chunk.UpdateMesh(item.Value, materialManager);
                yield return null;
            }

            generateChunks = false;

            StartCoroutine(CheckLoadRequirement());

            worldGenerated = true;
        }

        private async void LoadChunks() {
			ConcurrentDictionary<Vector3Int, Chunk> generatedData = new();
            ConcurrentDictionary<Vector3Int, ConcurrentDictionary<MaterialType, MeshData>> generatedMeshDatas = new();
            await Task.Run(() => {
                GenerateLoadData();

				foreach (var item in chunkToCreateCoordinates)
                    generatedData.TryAdd(item, chunkGenerator.Generate(item));

                foreach (var item in generatedData) {
                    foreach (var treeRoot in item.Value.TreeData.Positions) {
                        GenerateTree(CoordinateUtility.ToGlobal(item.Value.Coordinate, treeRoot));
                    }
                }

                foreach (var item in generatedData) {
                    ChunkUtility.ParallelFor((localBlockCoordinate) => {
                        if (item.Value.BlockMap[localBlockCoordinate] == BlockType.Water) {
                            Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(item.Key, localBlockCoordinate);
                            if (LiquidCalculator.GetFlowDirection(world, blockCoordinate) != Vector3Int.zero)
                                world.LiquidCalculatorWater.Add(blockCoordinate);
                        }
                    });
                }

                foreach (var item in sunlightCoordinatesToCalculate)
                    LightCalculator.AddSunlight(world, item);

                world.LightCalculatorSun.Calculate();
                foreach (var item in rendererToCreateCoordinates)
                    generatedMeshDatas.TryAdd(item, ChunkUtility.GenerateMeshData(world, world.GetChunk(item), blockDataProvider));
            }, cancellationTokenSource.Token);

            if (!cancellationTokenSource.IsCancellationRequested)
                StartCoroutine(GenerateChunks(world, generatedMeshDatas));
        }

        private void GenerateTree(Vector3Int blockCoordinate) {
            for (int i = 0; i < 5; i++) {
                Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                if (world.TryGetChunk(chunkCoordinate, out Chunk chunk)) {
                    Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                    chunk.BlockMap[localBlockCoordinate] = BlockType.Log;
                }

                blockCoordinate += Vector3Int.up;
            }

            for (int x = -2; x <= 2; x++) {
                for (int y = -2; y <= 2; y++) {
                    for (int z = -2; z <= 2; z++) {
                        Vector3Int leavesCoordinate = blockCoordinate + new Vector3Int(x, y, z);
						Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(leavesCoordinate);
						if (world.TryGetChunk(chunkCoordinate, out Chunk chunk)) {
							Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, leavesCoordinate);
                            if (!blockDataProvider.Get(chunk.BlockMap[localBlockCoordinate]).IsSolid)
							    chunk.BlockMap[localBlockCoordinate] = BlockType.Leaves;
						}
					}
                }
            }
        }

        private IEnumerator CheckLoadRequirement() {
            yield return new WaitForSeconds(loadCountdown);

            Vector3Int playerChunk = GetPlayerChunk();
            if (!generateChunks && (playerChunk.x != lastPlayerChunk.x || playerChunk.z != lastPlayerChunk.z)) {
                lastPlayerChunk = playerChunk;
                LoadChunks();
            } else
                StartCoroutine(CheckLoadRequirement());
        }

        private IEnumerator WaitForWorldStartGeneration() {
            lastPlayerChunk = GetPlayerChunk();
            LoadChunks();
            yield return new WaitUntil(() => worldGenerated);
            OnWorldCreate?.Invoke();
        }

        private void Start() {
            StartCoroutine(WaitForWorldStartGeneration());
        }

		private void OnDisable() {
            cancellationTokenSource.Cancel();
		}

		private void OnDestroy() {
            cancellationTokenSource.Dispose();
		}
	}
}