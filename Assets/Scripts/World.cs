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

        private Dictionary<Vector3Int, Chunk> chunks { get; set; } = new();
        public IReadOnlyDictionary<Vector3Int, Chunk> Chunks => chunks; 

        [SerializeField] 
        private Chunk chunk;

        public Chunk CreateChunk(Vector3Int coordinate)
        {
            var chunk = Instantiate(this.chunk, transform);
            chunk.Coordinate = coordinate;
            chunks.Add(coordinate, chunk);
            return chunk;
        }

        public Chunk GetChunk(Vector3Int coordinate)
        {
            if (chunks.ContainsKey(coordinate))
                return chunks[coordinate];

            return null;
        }

        public Chunk GetOrCreateChunk(Vector3Int coordinate)
        {
            if (chunks.ContainsKey(coordinate))
                return chunks[coordinate];

            return CreateChunk(coordinate);
        }

        public bool DestroyChunk(Vector3Int coordinate)
        {
            if (chunks.Remove(coordinate, out Chunk chunk))
            {
                Destroy(chunk.gameObject);
                return true;
            }

            return false;
        }

        public VoxelType GetVoxel(Vector3Int coordinate)
        {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(coordinate);
            if (chunks.TryGetValue(chunkCoordinate, out Chunk chunk))
            {
                Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
                return chunk.VoxelMap[localVoxelCoordinate];
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