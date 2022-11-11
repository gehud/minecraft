using Minecraft.Utilities;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Minecraft {
    public class ChunkLoader : Singleton<ChunkLoader> {
        public UnityEvent OnWorldCreate;

        [SerializeField]
        private Transform player;
        [SerializeField, Min(2)]
        private int drawDistance = 4;
        [SerializeField]
        private float loadCountdown = 0.5f;

        private Vector3Int lastPlayerChunk;
        private readonly ConcurrentStack<Vector3Int> chunkToRemoveCoordinates = new();
        private readonly ConcurrentStack<Vector3Int> chunkToCreateCoordinates = new();
        private readonly ConcurrentStack<Vector3Int> chunkDataToRemoveCoordinates = new();
        private readonly ConcurrentStack<Vector3Int> chunkDataToCreateCoordinates = new();
        private readonly ConcurrentStack<Vector2Int> sunlightCoordinatesToCalculate = new();

        bool generateChunks = false;
        bool worldGenerated = false;

        private Vector3Int GetPlayerChunk() {
            Vector3Int coordinate = CoordinateUtility.ToCoordinate(player.position);
            return CoordinateUtility.ToChunk(coordinate);
        }

        private void GenerateLoadData(World world) {
            chunkToRemoveCoordinates.Clear();
            chunkToCreateCoordinates.Clear();
            chunkDataToRemoveCoordinates.Clear();
            chunkDataToCreateCoordinates.Clear();
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
                            if (!world.Chunks.ContainsKey(chunkCoordinate))
                                chunkToCreateCoordinates.Push(chunkCoordinate);
                        }

                        if (!world.ChunkDatas.ContainsKey(chunkCoordinate)) {
                            chunkDataToCreateCoordinates.Push(chunkCoordinate);
                            sunlight = true;
                        }
                    }

                    if (sunlight) {
                        sunlightCoordinatesToCalculate.Push(new Vector2Int(x, z));
                    }
                }

            foreach (var item in world.ChunkDatas.Keys) {
                if (!loadedCoordinates.Contains(item))
                    chunkDataToRemoveCoordinates.Push(item);
            }

            foreach (var item in world.Chunks.Keys) {
                if (!visibleCoordinates.Contains(item))
                    chunkToRemoveCoordinates.Push(item);
            }
        }

        private IEnumerator GenerateChunks(World world, ConcurrentDictionary<Vector3Int, IDictionary<MaterialType, MeshData>> meshDatas) {
            generateChunks = true;

            foreach (var item in meshDatas) {
                Chunk chunk = world.GetOrCreateChunk(item.Key);
                chunk.UpdateMesh(item.Value);
                yield return null;
            }

            generateChunks = false;

            StartCoroutine(CheckLoadRequirement());

            worldGenerated = true;
        }

        private async void LoadChunks() {
            World world = World.Instance;
            ChunkDataGenerator chunkDataGenerator = ChunkDataGenerator.Instance;

            await Task.Run(() => GenerateLoadData(world));

            foreach (var item in chunkDataToRemoveCoordinates)
                world.ChunkDatas.Remove(item);

            foreach (var item in chunkToRemoveCoordinates) {
                world.Chunks.Remove(item, out Chunk chunk);
                Destroy(chunk.gameObject);
            }

            ConcurrentDictionary<Vector3Int, ChunkData> generatedData = new();
            await Task.Run(() => {
                foreach (var item in chunkDataToCreateCoordinates)
                    generatedData.TryAdd(item, chunkDataGenerator.GenerateChunkData(item));
            });
            foreach (var item in generatedData)
                world.ChunkDatas.Add(item.Key, item.Value);

            await Task.Run(() => {
                foreach (var item in generatedData) {
                    ChunkUtility.ForEachVoxel((localBlockCoordinate) => {
                        if (item.Value.BlockMap[localBlockCoordinate] == BlockType.Water) {
                            Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(item.Key, localBlockCoordinate);
                            world.LiquidCalculatorWater.Add(blockCoordinate, LiquidMap.MAX);
                        }
                    });
                }
            });

            await Task.Run(() => {
                foreach (var item in sunlightCoordinatesToCalculate)
                    LightCalculator.AddSunlight(world, item);
                world.LightCalculatorSun.Calculate();
            });

            ConcurrentDictionary<Vector3Int, IDictionary<MaterialType, MeshData>> generatedMeshDatas = new();
            await Task.Run(() => {
                foreach (var item in chunkToCreateCoordinates)
                    generatedMeshDatas.TryAdd(item, ChunkUtility.GenerateMeshData(world, world.ChunkDatas[item]));
            });

            StartCoroutine(GenerateChunks(world, generatedMeshDatas));
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