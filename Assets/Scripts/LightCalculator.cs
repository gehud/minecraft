using Minecraft.Utilities;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Profiling;

namespace Minecraft {
    public class LightCalculator {
        private struct Entry {
            public Vector3Int Coordinate;
            public byte Level;

            public Entry(Vector3Int coordinate, byte level) {
                Coordinate = coordinate;
                Level = level;
            }

            public Entry(int x, int y, int z, byte level) : this(new Vector3Int(x, y, z), level) { }
        }

        private readonly World world;
        private readonly LightChanel chanel;
        private readonly ConcurrentQueue<Entry> addQueue = new();
        private readonly ConcurrentQueue<Entry> removeQueue = new();

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

            var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            chunkData.LightMap.Set(localBlockCoordinate, chanel, level);
            chunkData.MarkDirty();
            world.ValidateChunkData(chunkCoordinate, localBlockCoordinate);

            var entry = new Entry(blockCoordinate, level);
            addQueue.Enqueue(entry);
        }

        public void Add(int x, int y, int z, byte level) {
            Add(new Vector3Int(x, y, z), level);
        }

        public void Add(Vector3Int blockCoordinate) {
            var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            var level = chunkData.LightMap.Get(localBlockCoordinate, chanel);
            if (level <= 1)
                return;

            var entry = new Entry(blockCoordinate, level);
            addQueue.Enqueue(entry);
        }

        public void Add(int x, int y, int z) {
            Add(new Vector3Int(x, y, z));
        }

        public void Remove(Vector3Int blockCoordinate) {
            var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            byte level = chunkData.LightMap.Get(localBlockCoordinate, chanel);
            if (level <= 1)
                return;

            chunkData.LightMap.Set(localBlockCoordinate, chanel, LightMap.MIN);
            world.ValidateChunkData(chunkCoordinate, localBlockCoordinate);
            chunkData.MarkDirty();

            var entry = new Entry(blockCoordinate, level);
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
            for (int x = startX; x <= endX; x++)
                for (int z = startZ; z <= endZ; z++) {
                    for (int y = World.HEIGHT * Chunk.SIZE - 1; y >= 0; y--) {
                        var blockCoordinate = new Vector3Int(x, y, z);
                        var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                        if (!world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                            break;
                        var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                        if (chunkData.BlockMap[localBlockCoordinate] != BlockType.Air)
                            break;
                        world.LightCalculatorSun.Add(blockCoordinate, LightMap.MAX);
                    }
                }
        }

        public void Calculate() {
            Profiler.BeginSample("LightCalculator.Calculate");

            while (removeQueue.TryDequeue(out Entry entry)) {
				for (int i = 0; i < blockSides.Length; i++) {
                    var blockCoordinate = entry.Coordinate + blockSides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                    var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                    if (world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                        var level = chunkData.LightMap.Get(localBlockCoordinate, chanel);
                        var blockType = chunkData.BlockMap[localBlockCoordinate];
                        var absorption = BlockDataManager.Data[blockType].Absorption;
                        if (level != 0 && level == entry.Level - absorption - 1) {
                            var removeEntry = new Entry(blockCoordinate, level);
                            removeQueue.Enqueue(removeEntry);
                            chunkData.LightMap.Set(localBlockCoordinate, chanel, LightMap.MIN);
                            chunkData.MarkDirty();
                            world.ValidateChunkData(chunkCoordinate, localBlockCoordinate);
                        } else if (level >= entry.Level) {
                            var addEntry = new Entry(blockCoordinate, level);
                            addQueue.Enqueue(addEntry);
                        }
                    }
                }
            }

            while (addQueue.TryDequeue(out Entry entry)) {
                if (entry.Level <= 1)
                    continue;

                for (int i = 0; i < blockSides.Length; i++) {
                    var blockCoordinate = entry.Coordinate + blockSides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                    var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                    if (world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                        var blockType = chunkData.BlockMap[localBlockCoordinate];
                        var absorption = BlockDataManager.Data[blockType].Absorption;
                        var level = chunkData.LightMap.Get(localBlockCoordinate, chanel);
                        if (BlockDataManager.Data[blockType].IsTransparent && level + absorption + 1 < entry.Level) {
                            var newLevel = (byte)(entry.Level - absorption - 1);
                            chunkData.LightMap.Set(localBlockCoordinate, chanel, newLevel);
                            var addEntry = new Entry(blockCoordinate, newLevel);
                            addQueue.Enqueue(addEntry);
                            chunkData.MarkDirty();
                            world.ValidateChunkData(chunkCoordinate, localBlockCoordinate);
                        }
                    }
                }
            }

            Profiler.EndSample();
        }
    }
}