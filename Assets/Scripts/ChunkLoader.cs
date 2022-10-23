using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Minecraft
{
    public class ChunkLoader : Singleton<ChunkLoader>
    {
        public UnityEvent OnWorldCreate;

        [SerializeField] 
        private Transform player;
        [SerializeField, Min(2)] 
        private int drawDistance = 4;
        [SerializeField, Min(0)]
        private float loadCountdown = 0.5f;

        private Vector3Int lastPlayerChunk;
        private ConcurrentBag<Vector3Int> loadedChunksCoordinates = new();
        private ConcurrentBag<Vector3Int> unloadedChunksCoordinates = new();
        private ConcurrentBag<Vector3Int> visibleChunksCoordinates = new();

        bool generateChunks = false;
        bool worldGenerated = false;

        private Vector3Int GetPlayerChunk()
        {
            Vector3Int globalVoxelCoordinate = CoordinateUtility.ToCoordinate(player.position);
            return CoordinateUtility.ToChunk(globalVoxelCoordinate);
        }

        private void GenerateLoadData(World world)
        {
            loadedChunksCoordinates.Clear();
            unloadedChunksCoordinates.Clear();
            visibleChunksCoordinates.Clear();
            int startX = lastPlayerChunk.x - (drawDistance + 1);
            int endX = lastPlayerChunk.x + drawDistance;
            int startZ = lastPlayerChunk.z - (drawDistance + 1);
            int endZ = lastPlayerChunk.z + drawDistance;
            for (int x = startX; x <= endX; x++)
                for (int z = startZ; z <= endZ; z++)
                {
                    for (int y = 0; y < World.HEIGHT; y++)
                    {
                        var chunkCoordinate = new Vector3Int(x, y, z);
                        loadedChunksCoordinates.Add(chunkCoordinate);
                        if (x != startX && x != endX && z != startZ && z != endZ)
                            visibleChunksCoordinates.Add(chunkCoordinate);
                    }
                }

            foreach (var item in world.Chunks)
            {
                if (!loadedChunksCoordinates.Contains(item.Key))
                    unloadedChunksCoordinates.Add(item.Key);
            }
        }

        private IEnumerator GenerateChunks(World world, ConcurrentDictionary<Vector3Int, Dictionary<MaterialType, MeshData>> data)
        {
            generateChunks = true;

            foreach (var item in unloadedChunksCoordinates)
            {
                world.DestroyChunk(item);
                yield return null;
            }

            foreach (var item in data)
            {
                Chunk chunk = world.GetOrCreateChunk(item.Key);
                chunk.UpdateMesh(item.Value);
                yield return null;
            }

            generateChunks = false;

            StartCoroutine(CheckLoadRequirement());

            worldGenerated = true;
        }

        private async void LoadChunks()
        {
            World world = World.Instance;

            await Task.Run(() => GenerateLoadData(world));

            foreach (var item in loadedChunksCoordinates)
                world.GetOrCreateChunkData(item);

            await Task.Run(() => 
            {
                foreach (var item in loadedChunksCoordinates)
                {
                    ChunkUtility.ForEachVoxel((x, y, z) =>
                    {
                        Vector3Int globalVoxelCoordinate = CoordinateUtility.ToGlobal(item, new Vector3Int(x, y, z));
                        if (globalVoxelCoordinate.y < 32)
                            world.GetChunkData(item).VoxelMap[x, y, z] = VoxelType.Stone;
                    });
                }
            });

            var data = new ConcurrentDictionary<Vector3Int, Dictionary<MaterialType, MeshData>>();
            await Task.Run(() =>
            {
                foreach (var item in visibleChunksCoordinates)
                {
                    var meshData = ChunkUtility.GenerateMeshData(world, world.GetChunkData(item));
                    data.TryAdd(item, meshData);
                }
            });

            StartCoroutine(GenerateChunks(world, data));
        }

        private IEnumerator CheckLoadRequirement()
        {
            yield return new WaitForSeconds(loadCountdown);

            Vector3Int playerChunk = GetPlayerChunk();
            if (!generateChunks && (playerChunk.x != lastPlayerChunk.x || playerChunk.z != lastPlayerChunk.z))
            {
                lastPlayerChunk = playerChunk;
                LoadChunks();
            }
            else
                StartCoroutine(CheckLoadRequirement());
        }

        private IEnumerator WaitForWorldStartGeneration()
        {
            lastPlayerChunk = GetPlayerChunk();
            LoadChunks();
            yield return new WaitUntil(() => worldGenerated);
            OnWorldCreate.Invoke();
        }

        private void Start()
        {
            StartCoroutine(WaitForWorldStartGeneration());
        }
    }
}