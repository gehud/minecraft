using System.Collections;
using System.Collections.Generic;
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
        private List<Vector3Int> loadedChunksCoordinates = new();
        private List<Vector3Int> unloadedChunksCoordinates = new();
        private List<Vector3Int> visibleChunksCoordinates = new();

        bool generateChunks = false;
        bool worldGenerated = false;

        private Vector3Int GetPlayerChunk()
        {
            Vector3Int globalVoxelCoordinate = CoordinateUtility.ToCoordinate(player.position);
            return CoordinateUtility.ToChunk(globalVoxelCoordinate);
        }

        private IEnumerator GenerateLoadData(World world)
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

                        yield return null;
                    }
                }

            foreach (var item in world.Chunks)
            {
                if (!loadedChunksCoordinates.Contains(item.Key))
                    unloadedChunksCoordinates.Add(item.Key);

                yield return null;
            }
        }

        private IEnumerator GenerateChunks(World world, Dictionary<Vector3Int, Dictionary<MaterialType, MeshData>> meshDatas)
        {
            generateChunks = true;

            foreach (var item in meshDatas)
            {
                Chunk chunk = world.GetChunk(item.Key);
                chunk.UpdateMesh(item.Value);
                yield return null;
            }

            generateChunks = false;

            StartCoroutine(CheckLoadRequirement());

            worldGenerated = true;
        }

        private IEnumerator LoadChunks()
        {
            World world = World.Instance;

            double start = Time.timeAsDouble;
            yield return StartCoroutine(GenerateLoadData(world));
            double stop = Time.timeAsDouble;
            Debug.Log(stop - start);

            foreach (var item in unloadedChunksCoordinates)
            {
                world.DestroyChunk(item);
                yield return null;
            }

            foreach (var item in loadedChunksCoordinates)
            {
                var chunk = world.GetOrCreateChunk(item);
                ChunkUtility.ForEachVoxel((x, y, z) =>
                {
                    Vector3Int globalVoxelCoordinate = CoordinateUtility.ToGlobal(item, new Vector3Int(x, y, z));
                    if (globalVoxelCoordinate.y < 32)
                        chunk.VoxelMap[x, y, z] = VoxelType.Stone;
                });
                yield return null;
            }

            var generatedMeshDatas = new Dictionary<Vector3Int, Dictionary<MaterialType, MeshData>>();
            foreach (var item in visibleChunksCoordinates)
            {
                generatedMeshDatas.TryAdd(item, ChunkUtility.GenerateMeshDatas(world, world.GetChunk(item)));
                yield return null;
            }

            StartCoroutine(GenerateChunks(world, generatedMeshDatas));
        }

        private IEnumerator CheckLoadRequirement()
        {
            yield return new WaitForSeconds(loadCountdown);

            Vector3Int playerChunk = GetPlayerChunk();
            if (!generateChunks && (playerChunk.x != lastPlayerChunk.x || playerChunk.z != lastPlayerChunk.z))
            {
                lastPlayerChunk = playerChunk;
                StartCoroutine(LoadChunks());
            }
            else
                StartCoroutine(CheckLoadRequirement());
        }

        private IEnumerator WaitForWorldStartGeneration()
        {
            lastPlayerChunk = GetPlayerChunk();
            StartCoroutine(LoadChunks());
            yield return new WaitUntil(() => worldGenerated);
            OnWorldCreate.Invoke();
        }

        private void Start()
        {
            StartCoroutine(WaitForWorldStartGeneration());
        }
    }
}