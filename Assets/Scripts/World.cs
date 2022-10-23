using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
    public class World : Singleton<World>
    {
        public const int HEIGHT = 16;

        [Serializable]
        private struct MaterialPair
        {
            public MaterialType Type;
            public Material Material;
        }

        [SerializeField] 
        private List<MaterialPair> materialPairs = new();

        private Dictionary<MaterialType, Material> materials = new();
        public IReadOnlyDictionary<MaterialType, Material> Materials => materials;

        [Serializable]
        private struct VoxelPair
        {
            public VoxelType Type;
            public VoxelData Data;
        }

        [SerializeField]
        private List<VoxelPair> voxelPairs = new();

        private Dictionary<VoxelType, VoxelData> voxels = new();
        public IReadOnlyDictionary<VoxelType, VoxelData> Voxels => voxels;

        private Dictionary<Vector3Int, ChunkData> chunkDatas = new();
        public IReadOnlyDictionary<Vector3Int, ChunkData> ChunkDatas => chunkDatas;

        private Dictionary<Vector3Int, Chunk> chunks = new();
        public IReadOnlyDictionary<Vector3Int, Chunk> Chunks => chunks;

        [SerializeField] 
        private Chunk chunk;

        public ChunkData CreateChunkData(Vector3Int coordinate)
        {
            var chunkData = new ChunkData(coordinate);
            chunkDatas.Add(coordinate, chunkData);
            return chunkData;
        }

        public Chunk CreateChunk(Vector3Int coordinate)
        {
            var chunk = Instantiate(this.chunk, transform);
            var chunkData = GetOrCreateChunkData(coordinate);
            chunk.Initialize(chunkData);
            chunks.Add(coordinate, chunk);
            return chunk;
        }

        public ChunkData GetChunkData(Vector3Int coordinate)
        {
            if (chunkDatas.ContainsKey(coordinate))
                return chunkDatas[coordinate];

            return null;
        }

        public Chunk GetChunk(Vector3Int coordinate)
        {
            if (chunks.ContainsKey(coordinate))
                return chunks[coordinate];

            return null;
        }

        public ChunkData GetOrCreateChunkData(Vector3Int coordinate)
        {
            if (chunkDatas.ContainsKey(coordinate))
                return chunkDatas[coordinate];

            return CreateChunkData(coordinate);
        }

        public Chunk GetOrCreateChunk(Vector3Int coordinate)
        {
            if (chunks.ContainsKey(coordinate))
                return chunks[coordinate];

            return CreateChunk(coordinate);
        }

        public void DestroyChunk(Vector3Int coordinate)
        {
            chunkDatas.Remove(coordinate);
            chunks.Remove(coordinate, out Chunk chunk);
            Destroy(chunk.gameObject);
        }

        public VoxelType GetVoxel(Vector3Int coordinate)
        {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(coordinate);
            if (chunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData))
            {
                Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
                return chunkData.VoxelMap[localVoxelCoordinate];
            }

            return VoxelType.Air;
        }

        private void Awake()
        {
            foreach (var item in materialPairs)
                materials.Add(item.Type, item.Material);
            foreach (var item in voxelPairs)
                voxels.Add(item.Type, item.Data);
        }

        private void Update()
        {
            foreach (var chunk in chunks.Values)
                chunk.Handle();
        }
    }
}