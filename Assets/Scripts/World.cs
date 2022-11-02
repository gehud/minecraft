using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft {
    public class World : Singleton<World> {
        public const int HEIGHT = 16;

        private Dictionary<Vector3Int, ChunkData> chunkDatas = new();
        public Dictionary<Vector3Int, ChunkData> ChunkDatas => chunkDatas;

        private Dictionary<Vector3Int, Chunk> chunks = new();
        public Dictionary<Vector3Int, Chunk> Chunks => chunks;

        public LightCalculator LightCalculatorSun { get; set; }
        public LightCalculator LightCalculatorRed { get; set; }
        public LightCalculator LightCalculatorGreen { get; set; }
        public LightCalculator LightCalculatorBlue { get; set; }

        public LiquidCalculator LiquidCalculatorWater { get; set; }

        [SerializeField]
        private Chunk chunk;

        [SerializeField, Min(0)]
        private float tick = 0.25f;

        public ChunkData CreateChunkData(Vector3Int coordinate) {
            var chunkData = new ChunkData {
                Coordinate = coordinate
            };
            chunkDatas.Add(coordinate, chunkData);
            return chunkData;
        }

        public Chunk CreateChunk(Vector3Int coordinate) {
            var chunk = Instantiate(this.chunk, transform);
            var chunkData = GetOrCreateChunkData(coordinate);
            chunk.Initialize(chunkData);
            chunks.Add(coordinate, chunk);
            return chunk;
        }

        public ChunkData GetChunkData(Vector3Int coordinate) {
            if (chunkDatas.ContainsKey(coordinate))
                return chunkDatas[coordinate];

            return null;
        }

        public Chunk GetChunk(Vector3Int coordinate) {
            if (chunks.ContainsKey(coordinate))
                return chunks[coordinate];

            return null;
        }

        public ChunkData GetOrCreateChunkData(Vector3Int coordinate) {
            if (chunkDatas.ContainsKey(coordinate))
                return chunkDatas[coordinate];

            return CreateChunkData(coordinate);
        }

        public Chunk GetOrCreateChunk(Vector3Int coordinate) {
            if (chunks.ContainsKey(coordinate))
                return chunks[coordinate];

            return CreateChunk(coordinate);
        }

        public void DestroyChunk(Vector3Int coordinate) {
            chunkDatas.Remove(coordinate);
            chunks.Remove(coordinate, out Chunk chunk);
            Destroy(chunk.gameObject);
        }

        public BlockType GetVoxel(Vector3Int coordinate) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(coordinate);
            if (chunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
                return chunkData.BlockMap[localVoxelCoordinate];
            }

            return BlockType.Air;
        }

        public BlockType GetVoxel(int x, int y, int z) {
            return GetVoxel(new Vector3Int(x, y, z));
        }

        public void SetVoxel(Vector3Int globalVoxelCoordinate, BlockType voxelType) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
            Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, globalVoxelCoordinate);
            if (ChunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                chunkData.BlockMap[localVoxelCoordinate] = voxelType;
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

        public int GetLight(Vector3Int coordinate, LightChanel chanel) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(coordinate);
            if (chunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
                return chunkData.LightMap.Get(localVoxelCoordinate, chanel);
            }

            return LightMap.MAX;
        }

        public byte GetLiquidAmount(Vector3Int coordinate) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(coordinate);
            if (chunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
                return chunkData.LiquidMap[localVoxelCoordinate].Amount;
            }

            return LiquidMap.MIN;
        }

        public Liquid GetLiquid(Vector3Int coordinate) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(coordinate);
            if (chunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
                return chunkData.LiquidMap[localVoxelCoordinate];
            }

            return Liquid.Empty;
        }

        public Liquid GetLiquid(int x, int y, int z) {
            return GetLiquid(new Vector3Int(x, y, z));
        }

        public byte GetLiquidAmount(Vector3Int coordinate, BlockType type) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(coordinate);
            if (chunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, coordinate);
                return chunkData.LiquidMap.Get(localVoxelCoordinate, type);
            }

            return LiquidMap.MIN;
        }

        public void DestroyVoxel(Vector3Int coordinate) {
            var blockDataManager = BlockManager.Instance;

            SetVoxel(coordinate, BlockType.Air);

            LiquidCalculatorWater.Remove(coordinate);

            LightCalculatorRed.Remove(coordinate);
            LightCalculatorGreen.Remove(coordinate);
            LightCalculatorBlue.Remove(coordinate);
            LightCalculatorRed.Calculate();
            LightCalculatorGreen.Calculate();
            LightCalculatorBlue.Calculate();

            if (blockDataManager.Blocks[GetVoxel(coordinate + Vector3Int.up)].IsTransparent
                && GetLight(coordinate + Vector3Int.up, LightChanel.Sun) == LightMap.MAX) {
                for (int y = coordinate.y; y >= 0; y--) {
                    LightCalculatorSun.Add(coordinate.x, y, coordinate.z, LightMap.MAX);
                    if (!blockDataManager.Blocks[GetVoxel(new Vector3Int(coordinate.x, y - 1, coordinate.z))].IsTransparent)
                        break;
                }
            }

            LiquidCalculatorWater.Add(coordinate + Vector3Int.right);
            LiquidCalculatorWater.Add(coordinate + Vector3Int.left);
            LiquidCalculatorWater.Add(coordinate + Vector3Int.up);
            LiquidCalculatorWater.Add(coordinate + Vector3Int.down);
            LiquidCalculatorWater.Add(coordinate + Vector3Int.forward);
            LiquidCalculatorWater.Add(coordinate + Vector3Int.back);

            LightCalculatorRed.Add(coordinate + Vector3Int.right);
            LightCalculatorRed.Add(coordinate + Vector3Int.left);
            LightCalculatorRed.Add(coordinate + Vector3Int.up);
            LightCalculatorRed.Add(coordinate + Vector3Int.down);
            LightCalculatorRed.Add(coordinate + Vector3Int.forward);
            LightCalculatorRed.Add(coordinate + Vector3Int.back);
            LightCalculatorRed.Calculate();

            LightCalculatorGreen.Add(coordinate + Vector3Int.right);
            LightCalculatorGreen.Add(coordinate + Vector3Int.left);
            LightCalculatorGreen.Add(coordinate + Vector3Int.up);
            LightCalculatorGreen.Add(coordinate + Vector3Int.down);
            LightCalculatorGreen.Add(coordinate + Vector3Int.forward);
            LightCalculatorGreen.Add(coordinate + Vector3Int.back);
            LightCalculatorGreen.Calculate();

            LightCalculatorBlue.Add(coordinate + Vector3Int.right);
            LightCalculatorBlue.Add(coordinate + Vector3Int.left);
            LightCalculatorBlue.Add(coordinate + Vector3Int.up);
            LightCalculatorBlue.Add(coordinate + Vector3Int.down);
            LightCalculatorBlue.Add(coordinate + Vector3Int.forward);
            LightCalculatorBlue.Add(coordinate + Vector3Int.back);
            LightCalculatorBlue.Calculate();

            LightCalculatorSun.Add(coordinate + Vector3Int.right);
            LightCalculatorSun.Add(coordinate + Vector3Int.left);
            LightCalculatorSun.Add(coordinate + Vector3Int.up);
            LightCalculatorSun.Add(coordinate + Vector3Int.down);
            LightCalculatorSun.Add(coordinate + Vector3Int.forward);
            LightCalculatorSun.Add(coordinate + Vector3Int.back);
            LightCalculatorSun.Calculate();
        }

        public void PlaceVoxel(Vector3Int coordinate, BlockType voxelType) {
            var blockDataManager = BlockManager.Instance;

            LiquidCalculatorWater.Remove(coordinate);

            SetVoxel(coordinate, voxelType);

            LightCalculatorRed.Remove(coordinate);
            LightCalculatorGreen.Remove(coordinate);
            LightCalculatorBlue.Remove(coordinate);
            for (int y = coordinate.y; y >= 0; y--) {
                LightCalculatorSun.Remove(coordinate.x, y, coordinate.z);
                if (!blockDataManager.Blocks[GetVoxel(new Vector3Int(coordinate.x, y - 1, coordinate.z))].IsTransparent)
                    break;
            }
            LightCalculatorRed.Calculate();
            LightCalculatorGreen.Calculate();
            LightCalculatorBlue.Calculate();
            LightCalculatorSun.Calculate();

            LightColor emission = blockDataManager.Blocks[voxelType].Emission;
            if (emission.R != 0) {
                LightCalculatorRed.Add(coordinate, emission.R);
                LightCalculatorRed.Calculate();
            }
            if (emission.G != 0) {
                LightCalculatorGreen.Add(coordinate, emission.G);
                LightCalculatorGreen.Calculate();
            }
            if (emission.B != 0) {
                LightCalculatorBlue.Add(coordinate, emission.B);
                LightCalculatorBlue.Calculate();
            }

            if (voxelType == BlockType.Water) {
                LiquidCalculatorWater.Add(coordinate, LiquidMap.MAX);
            }
        }

        private void Awake() {
            LightCalculatorRed = new LightCalculator(this, LightChanel.Red);
            LightCalculatorGreen = new LightCalculator(this, LightChanel.Green);
            LightCalculatorBlue = new LightCalculator(this, LightChanel.Blue);
            LightCalculatorSun = new LightCalculator(this, LightChanel.Sun);
            LiquidCalculatorWater = new LiquidCalculator(this, BlockType.Water);
        }

        private IEnumerator LiquidCalculation() {
            while (true) {
                LiquidCalculatorWater.Calculate();
                yield return new WaitForSeconds(tick);
            }
        }

        private void Start() {
            StartCoroutine(LiquidCalculation());
        }

        private void Update() {
            foreach (var chunk in chunks.Values) {
                if (chunk.Data.IsComplete && chunk.Data.IsDirty)
                    chunk.UpdateMesh(ChunkUtility.GenerateMeshData(this, chunk.Data));
            }
        }
    }
}