using Minecraft.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft {
    public class LightCalculator {
        private struct Entry {
            public Vector3Int Coordinate;
            public byte Level;

            public Entry(Vector3Int coordinate, byte level) {
                Coordinate= coordinate;
                Level = level;
            }

            public Entry(int x, int y, int z, byte level) : this(new Vector3Int(x, y, z), level) { }
        }

        private readonly World world;
        private readonly LightChanel chanel;
        private readonly Queue<Entry> addQueue = new();
        private readonly Queue<Entry> removeQueue = new();

        private static BlockDataManager BlockDataManager { get; set; }
         
        private static readonly Vector3Int[] blockSides = {
            new Vector3Int( 0,  0,  1),
            new Vector3Int( 0,  0, -1),
            new Vector3Int( 0,  1,  0),
            new Vector3Int( 0, -1,  0),
            new Vector3Int( 1,  0,  0),
            new Vector3Int(-1,  0,  0),
        };

        public static void SetBlockDataManager(BlockDataManager blockDataManager) {
            BlockDataManager = blockDataManager;
        }

        public LightCalculator(World world, LightChanel chanel) {
            this.world = world;
            this.chanel = chanel;
        }

        public void Add(Vector3Int blockCoordinate, byte level) {
            if (level <= 1)
                return;

            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            chunkData.LightMap.Set(localBlockCoordinate, chanel, level);
            chunkData.IsDirty = true;

            Entry entry = new(blockCoordinate, level);
            addQueue.Enqueue(entry);
        }

        public void Add(int x, int y, int z, byte level) {
            Add(new Vector3Int(x, y, z), level);
        }

        public void Add(Vector3Int blockCoordinate) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            chunkData.IsDirty = true;

            Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            byte level = chunkData.LightMap.Get(localBlockCoordinate, chanel);
            if (level <= 1)
                return;

            Entry entry = new(blockCoordinate, level);
            addQueue.Enqueue(entry);
        }

        public void Add(int x, int y, int z) {
            Add(new Vector3Int(x, y, z));
        }

        public void Remove(Vector3Int blockCoordinate) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            byte level = chunkData.LightMap.Get(localBlockCoordinate, chanel);
            if (level <= 1)
                return;

            chunkData.LightMap.Set(localBlockCoordinate, chanel, LightMap.MIN);
            chunkData.IsDirty = true;

            Entry entry = new(blockCoordinate, level);
            removeQueue.Enqueue(entry);
        }

        public void Remove(int x, int y, int z) {
            Remove(new Vector3Int(x, y, z));
        }

        public static void AddSunlight(World world, Vector2Int column) {
            int startX = column.x * Chunk.SIZE;
            int endX = column.x * Chunk.SIZE + Chunk.SIZE - 1;
            int startZ = column.y * Chunk.SIZE;
            int endZ = column.y * Chunk.SIZE + Chunk.SIZE - 1;

            // Add sunlight.
            for (int x = startX; x <= endX; x++)
                for (int z = startZ; z <= endZ; z++) {
                    for (int y = World.HEIGHT * Chunk.SIZE - 1; y >= 0; y--) {
                        Vector3Int blockCoordinate = new(x, y, z);
                        Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                        if (!world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                            break;
                        Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                        if (!BlockDataManager.Data[chunkData.BlockMap[localBlockCoordinate]].IsTransparent)
                            break;
                        chunkData.LightMap.SetSun(localBlockCoordinate, LightMap.MAX);
                    }
                }

            // Receive sunlight.
            for (int x = startX; x <= endX; x++)
                for (int z = startZ; z <= endZ; z++) {
                    for (int y = World.HEIGHT * Chunk.SIZE - 1; y >= 0; y--) {
                        Vector3Int blockCoordinate = new(x, y, z);
                        Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                        if (!world.ChunksData.ContainsKey(chunkCoordinate))
                            break;
                        if (!BlockDataManager.Data[world.GetBlock(blockCoordinate)].IsTransparent) {
                            for (int newY = y - 1; newY >= 0; newY--) {
                                blockCoordinate = new Vector3Int(x, newY, z);
                                chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                                if (!BlockDataManager.Data[world.GetBlock(blockCoordinate)].IsTransparent)
                                    continue;
                                if (world.ChunksData.ContainsKey(chunkCoordinate + Vector3Int.right)
                                    && world.GetLightLevel(blockCoordinate + Vector3Int.right, LightChanel.Sun) == LightMap.MAX)
                                    world.LightCalculatorSun.Add(blockCoordinate + Vector3Int.right);
                                if (world.ChunksData.ContainsKey(chunkCoordinate + Vector3Int.left)
                                    && world.GetLightLevel(blockCoordinate + Vector3Int.left, LightChanel.Sun) == LightMap.MAX)
                                    world.LightCalculatorSun.Add(blockCoordinate + Vector3Int.left);
                                if (world.ChunksData.ContainsKey(chunkCoordinate + Vector3Int.forward)
                                    && world.GetLightLevel(blockCoordinate + Vector3Int.forward, LightChanel.Sun) == LightMap.MAX)
                                    world.LightCalculatorSun.Add(blockCoordinate + Vector3Int.forward);
                                if (world.ChunksData.ContainsKey(chunkCoordinate + Vector3Int.back)
                                    && world.GetLightLevel(blockCoordinate + Vector3Int.back, LightChanel.Sun) == LightMap.MAX)
                                    world.LightCalculatorSun.Add(blockCoordinate + Vector3Int.back);
                            }
                            break;
                        }
                    }
                }
        }

        public void Calculate() {
            while (removeQueue.TryDequeue(out Entry entry)) {
                foreach (var side in blockSides) {
                    int x = entry.Coordinate.x + side.x;
                    int y = entry.Coordinate.y + side.y;
                    int z = entry.Coordinate.z + side.z;
                    Vector3Int blockCoordinate = new(x, y, z);
                    Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                    Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                    if (world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                        chunkData.IsDirty = true;
                        byte level = chunkData.LightMap.Get(localBlockCoordinate, chanel);
                        if (level != 0 && level == entry.Level - 1) {
                            Entry removeEntry = new(x, y, z, level);
                            removeQueue.Enqueue(removeEntry);
                            chunkData.LightMap.Set(localBlockCoordinate, chanel, LightMap.MIN);
                        } else if (level >= entry.Level) {
                            Entry addEntry = new(x, y, z, level);
                            addQueue.Enqueue(addEntry);
                        }
                    }
                }
            }

            while (addQueue.TryDequeue(out Entry entry)) {
                if (entry.Level <= 1)
                    continue;

                foreach (var side in blockSides) {
                    int x = entry.Coordinate.x + side.x;
                    int y = entry.Coordinate.y + side.y;
                    int z = entry.Coordinate.z + side.z;
                    Vector3Int blockCoordinate = new(x, y, z);
                    Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                    Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                    if (world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                        chunkData.IsDirty = true;
                        BlockType voxelType = chunkData.BlockMap[localBlockCoordinate];
                        byte level = chunkData.LightMap.Get(localBlockCoordinate, chanel);
                        if (BlockDataManager.Data[voxelType].IsTransparent && level + 2 <= entry.Level) {
                            chunkData.LightMap.Set(localBlockCoordinate, chanel, (byte)(entry.Level - 1));
                            Entry addEntry = new(x, y, z, (byte)(entry.Level - 1));
                            addQueue.Enqueue(addEntry);
                        }
                    }
                }
            }
        }
    }
}