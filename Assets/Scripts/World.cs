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
        public Dictionary<Vector3Int, ChunkData> ChunkDatas => chunkDatas;

        private Dictionary<Vector3Int, Chunk> chunks = new();
        public Dictionary<Vector3Int, Chunk> Chunks => chunks;

        public LightMapCalculator LightMapCalculatorSun { get; set; }
        public LightMapCalculator LightMapCalculatorRed { get; set; }
        public LightMapCalculator LightMapCalculatorGreen { get; set; }
        public LightMapCalculator LightMapCalculatorBlue { get; set; }

        [SerializeField] 
        private Chunk chunk;

        public ChunkData CreateChunkData(Vector3Int coordinate)
        {
            var chunkData = new ChunkData
            {
                Coordinate = coordinate
            };
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

        public void SetVoxel(Vector3Int globalVoxelCoordinate, VoxelType voxelType)
        {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
            Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, globalVoxelCoordinate);
            if (ChunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData))
            {
                chunkData.VoxelMap[localVoxelCoordinate] = voxelType;
                chunkData.IsDirty = true;
            }
            if (localVoxelCoordinate.x == 0 && ChunkDatas.TryGetValue(chunkCoordinate + Vector3Int.left, out chunkData))
                chunkData.IsDirty = true;
            if (localVoxelCoordinate.y == 0 && ChunkDatas.TryGetValue(chunkCoordinate + Vector3Int.down, out chunkData))
                chunkData.IsDirty = true;
            if (localVoxelCoordinate.z == 0 && ChunkDatas.TryGetValue(chunkCoordinate + Vector3Int.back, out chunkData))
                chunkData.IsDirty = true;
            if (localVoxelCoordinate.x == Chunk.SIZE - 1 && ChunkDatas.TryGetValue(chunkCoordinate + Vector3Int.right, out chunkData))
                chunkData.IsDirty = true;
            if (localVoxelCoordinate.y == Chunk.SIZE - 1 && ChunkDatas.TryGetValue(chunkCoordinate + Vector3Int.up, out chunkData))
                chunkData.IsDirty = true;
            if (localVoxelCoordinate.z == Chunk.SIZE - 1 && ChunkDatas.TryGetValue(chunkCoordinate + Vector3Int.forward, out chunkData))
                chunkData.IsDirty = true;
        }

        public int GetLight(Vector3Int coordinate, LightMap.Chanel chanel)
        {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(coordinate);
            if (chunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData))
            {
                Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
                return chunkData.LightMap.Get(localVoxelCoordinate, chanel);
            }

            return LightMap.MAX;
        }

        public void DestroyVoxel(Vector3Int coordinate)
        {
            SetVoxel(coordinate, VoxelType.Air);
            LightMapCalculatorRed.Remove(coordinate);
            LightMapCalculatorGreen.Remove(coordinate);
            LightMapCalculatorBlue.Remove(coordinate);
            LightMapCalculatorRed.Calculate();
            LightMapCalculatorGreen.Calculate();
            LightMapCalculatorBlue.Calculate();

            if (Voxels[GetVoxel(coordinate + Vector3Int.up)].IsTransparent
                && GetLight(coordinate + Vector3Int.up, LightMap.Chanel.Sun) == LightMap.MAX)
            {
                for (int y = coordinate.y; y >= 0; y--)
                {
                    LightMapCalculatorSun.Add(coordinate.x, y, coordinate.z, LightMap.MAX);
                    if (!Voxels[GetVoxel(new Vector3Int(coordinate.x, y - 1, coordinate.z))].IsTransparent)
                        break;
                }
            }

            LightMapCalculatorRed.Add(coordinate + Vector3Int.right);
            LightMapCalculatorRed.Add(coordinate + Vector3Int.left);
            LightMapCalculatorRed.Add(coordinate + Vector3Int.up);
            LightMapCalculatorRed.Add(coordinate + Vector3Int.down);
            LightMapCalculatorRed.Add(coordinate + Vector3Int.forward);
            LightMapCalculatorRed.Add(coordinate + Vector3Int.back);
            LightMapCalculatorRed.Calculate();

            LightMapCalculatorGreen.Add(coordinate + Vector3Int.right);
            LightMapCalculatorGreen.Add(coordinate + Vector3Int.left);
            LightMapCalculatorGreen.Add(coordinate + Vector3Int.up);
            LightMapCalculatorGreen.Add(coordinate + Vector3Int.down);
            LightMapCalculatorGreen.Add(coordinate + Vector3Int.forward);
            LightMapCalculatorGreen.Add(coordinate + Vector3Int.back);
            LightMapCalculatorGreen.Calculate();

            LightMapCalculatorBlue.Add(coordinate + Vector3Int.right);
            LightMapCalculatorBlue.Add(coordinate + Vector3Int.left);
            LightMapCalculatorBlue.Add(coordinate + Vector3Int.up);
            LightMapCalculatorBlue.Add(coordinate + Vector3Int.down);
            LightMapCalculatorBlue.Add(coordinate + Vector3Int.forward);
            LightMapCalculatorBlue.Add(coordinate + Vector3Int.back);
            LightMapCalculatorBlue.Calculate();

            LightMapCalculatorSun.Add(coordinate + Vector3Int.right);
            LightMapCalculatorSun.Add(coordinate + Vector3Int.left);
            LightMapCalculatorSun.Add(coordinate + Vector3Int.up);
            LightMapCalculatorSun.Add(coordinate + Vector3Int.down);
            LightMapCalculatorSun.Add(coordinate + Vector3Int.forward);
            LightMapCalculatorSun.Add(coordinate + Vector3Int.back);
            LightMapCalculatorSun.Calculate();
        }

        public void PlaceVoxel(Vector3Int coordinate, VoxelType voxelType)
        {
            SetVoxel(coordinate, voxelType);

            LightMapCalculatorRed.Remove(coordinate);
            LightMapCalculatorGreen.Remove(coordinate);
            LightMapCalculatorBlue.Remove(coordinate);
            for (int y = coordinate.y; y >= 0; y--)
            {
                LightMapCalculatorSun.Remove(coordinate.x, y, coordinate.z);
                if (!Voxels[GetVoxel(new Vector3Int(coordinate.x, y - 1, coordinate.z))].IsTransparent)
                    break;
            }
            LightMapCalculatorRed.Calculate();
            LightMapCalculatorGreen.Calculate();
            LightMapCalculatorBlue.Calculate();
            LightMapCalculatorSun.Calculate();

            LightColor emission = Voxels[voxelType].Emission;
            if (emission.R != 0)
            {
                LightMapCalculatorRed.Add(coordinate, emission.R);
                LightMapCalculatorRed.Calculate();
            }
            if (emission.G != 0)
            {
                LightMapCalculatorGreen.Add(coordinate, emission.G);
                LightMapCalculatorGreen.Calculate();
            }
            if (emission.B != 0)
            {
                LightMapCalculatorBlue.Add(coordinate, emission.B);
                LightMapCalculatorBlue.Calculate();
            }
        }

        private void Awake()
        {
            foreach (var item in materialPairs)
                materials.Add(item.Type, item.Material);
            foreach (var item in voxelPairs)
                voxels.Add(item.Type, item.Data);
            LightMapCalculatorRed = new LightMapCalculator(this, LightMap.Chanel.Red);
            LightMapCalculatorGreen = new LightMapCalculator(this, LightMap.Chanel.Green);
            LightMapCalculatorBlue = new LightMapCalculator(this, LightMap.Chanel.Blue);
            LightMapCalculatorSun = new LightMapCalculator(this, LightMap.Chanel.Sun);
        }

        private void Update()
        {
            foreach (var chunk in chunks.Values)
                chunk.Handle();
        }
    }
}