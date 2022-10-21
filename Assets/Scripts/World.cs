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
            return chunks.GetValueOrDefault(coordinate);
        }

        public bool DestroyChunk(Vector3Int coordinate)
        {
            if (chunks.Remove(coordinate, out Chunk chunk))
            {
                Destroy(chunk);
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

        private void Start()
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = -1; x < 1; x++)
                {
                    for (int z = -1; z < 1; z++)
                    {
                        Vector3Int chunkCoordinate = new(x, y, z);
                        var chunk = CreateChunk(chunkCoordinate);
                        ChunkUtility.ForEachVoxel((x, y, z) => 
                        {
                            if (chunkCoordinate.y * Chunk.SIZE + y < 32)
                                chunk.VoxelMap[x, y, z] = VoxelType.Stone;
                        });
                    }
                }
            }
        }

        private void Update()
        {
            foreach (var item in chunks)
            {
                if (item.Value.IsDirty)
                    item.Value.UpdateMesh();
            }
        }
    }
}