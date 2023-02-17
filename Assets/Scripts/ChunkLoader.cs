using Minecraft.Utilities;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

namespace Minecraft {
    public class ChunkLoader : MonoBehaviour {
        public UnityEvent OnWorldCreate;

        [SerializeField]
        private Transform player;
        [SerializeField, Min(2)]
        private int drawDistance = 2;
        [SerializeField]
        private float loadCountdown = 0.5f;

        private Vector3Int lastPlayerChunk;
        private readonly ConcurrentStack<Vector3Int> chunkToDestroyCoordinates = new();
        private readonly ConcurrentStack<Vector3Int> rendererToDestroyCoordinates = new();
        private readonly ConcurrentStack<Vector3Int> chunkToCreateCoordinates = new();
        private readonly ConcurrentStack<Vector3Int> rendererToCreateCoordinates = new();
        private readonly ConcurrentStack<Vector2Int> sunlightCoordinatesToCalculate = new();

        bool generateChunks = false;
        bool worldGenerated = false;

        [Inject]
        private World World { get; }

        [Inject]
        private BlockDataProvider BlockDataProvider { get; }

        [Inject]
        private MaterialManager MaterialManager { get; }

        [Inject]
        private ChunkDataGenerator ChunkDataGenerator { get; }

        private Vector3Int GetPlayerChunk() {
            Vector3Int coordinate = CoordinateUtility.ToCoordinate(player.position);
            return CoordinateUtility.ToChunk(coordinate);
        }

        private void GenerateLoadData() {
            chunkToDestroyCoordinates.Clear();
            rendererToDestroyCoordinates.Clear();
            chunkToCreateCoordinates.Clear();
            rendererToCreateCoordinates.Clear();
            sunlightCoordinatesToCalculate.Clear();
            ConcurrentStack<Vector3Int> loadedCoordinates = new();
            ConcurrentStack<Vector3Int> visibleCoordinates = new();
            int startX = lastPlayerChunk.x - (drawDistance + 1);
            int endX = lastPlayerChunk.x + drawDistance;
            int startZ = lastPlayerChunk.z - (drawDistance + 1);
            int endZ = lastPlayerChunk.z + drawDistance;
            for (int x = startX; x <= endX; x++)
                for (int z = startZ; z <= endZ; z++) {
                    bool sunlight = false;
                    for (int y = 0; y < World.HEIGHT; y++) {
                        Vector3Int chunkCoordinate = new Vector3Int(x, y, z);
                        loadedCoordinates.Push(chunkCoordinate);
                        if (x != startX && x != endX && z != startZ && z != endZ) {
                            visibleCoordinates.Push(chunkCoordinate);
                            if (!World.HasRenderer(chunkCoordinate))
                                rendererToCreateCoordinates.Push(chunkCoordinate);
                        }

                        if (!World.HasChunk(chunkCoordinate)) {
                            chunkToCreateCoordinates.Push(chunkCoordinate);
                            sunlight = true;
                        }
                    }

                    if (sunlight) {
                        sunlightCoordinatesToCalculate.Push(new Vector2Int(x, z));
                    }
                }

            foreach (var item in World.Renderers) {
                if (!visibleCoordinates.Contains(item.Key))
                    rendererToDestroyCoordinates.Push(item.Key);
            }

			foreach (var item in World.Chunks) {
				if (!loadedCoordinates.Contains(item.Key))
					chunkToDestroyCoordinates.Push(item.Key);
			}
		}

        private IEnumerator GenerateChunks(World world, ConcurrentDictionary<Vector3Int, ConcurrentDictionary<MaterialType, MeshData>> meshDatas) {
            generateChunks = true;

			foreach (var item in chunkToDestroyCoordinates) {
				World.DestroyChunk(item);
                yield return null;
			}

			foreach (var item in rendererToDestroyCoordinates) {
				World.DestroyRenderer(item);
                yield return null;
			}

			foreach (var item in meshDatas) {
                ChunkRenderer chunk = world.GetOrCreateRenderer(item.Key);
                chunk.UpdateMesh(item.Value, MaterialManager);
                yield return null;
            }

            generateChunks = false;

            StartCoroutine(CheckLoadRequirement());

            worldGenerated = true;
        }

        private async void LoadChunks() {
            await Task.Run(() => GenerateLoadData());

			ConcurrentDictionary<Vector3Int, Chunk> generatedData = new();
            await Task.Run(() => {
                foreach (var item in chunkToCreateCoordinates)
                    generatedData.TryAdd(item, ChunkDataGenerator.GenerateChunkData(item));
            });

            foreach (var item in generatedData)
                World.SetChunk(item.Key, item.Value);

            await Task.Run(() => { 
                foreach (var item in generatedData) {
                    foreach (var treeRoot in item.Value.TreeData.Positions) {
                        GenerateTree(CoordinateUtility.ToGlobal(item.Value.Coordinate, treeRoot));
                    }
                }
            });

            await Task.Run(() => {
                foreach (var item in generatedData) {
                    ChunkUtility.For((localBlockCoordinate) => {
                        if (item.Value.BlockMap[localBlockCoordinate] == BlockType.Water) {
                            Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(item.Key, localBlockCoordinate);
                            World.LiquidCalculatorWater.Add(blockCoordinate);
                        }
                    });
                }
            });

            await Task.Run(() => {
                foreach (var item in sunlightCoordinatesToCalculate)
                    LightCalculator.AddSunlight(World, item);
                World.LightCalculatorSun.Calculate();
            });

            ConcurrentDictionary<Vector3Int, ConcurrentDictionary<MaterialType, MeshData>> generatedMeshDatas = new();
            await Task.Run(() => {
                foreach (var item in rendererToCreateCoordinates)
                    generatedMeshDatas.TryAdd(item, ChunkUtility.GenerateMeshData(World, World.GetChunk(item), BlockDataProvider));
            });

            StartCoroutine(GenerateChunks(World, generatedMeshDatas));
        }

        private void GenerateTree(Vector3Int blockCoordinate) {
            for (int i = 0; i < 5; i++) {
                Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                if (World.TryGetChunk(chunkCoordinate, out Chunk chunk)) {
                    Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                    chunk.BlockMap[localBlockCoordinate] = BlockType.Log;
                }

                blockCoordinate += Vector3Int.up;
            }

            for (int x = -2; x <= 2; x++)
                for (int y = -2; y <= 2; y++)
                    for (int z = -2; z <= 2; z++) {
                        Vector3Int leavesCoordinate = blockCoordinate + new Vector3Int(x, y, z);
						Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(leavesCoordinate);
						if (World.TryGetChunk(chunkCoordinate, out Chunk chunk)) {
							Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, leavesCoordinate);
                            if (!BlockDataProvider.Get(chunk.BlockMap[localBlockCoordinate]).IsSolid)
							    chunk.BlockMap[localBlockCoordinate] = BlockType.Leaves;
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
    }
}