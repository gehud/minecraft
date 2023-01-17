using Minecraft.Utilities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

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

            var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            chunkData.LightMap.Set(localBlockCoordinate, chanel, level);
			world.ValidateChunkData(chunkCoordinate, localBlockCoordinate);
			chunkData.MarkDirty();

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
            byte level = chunkData.LightMap.Get(localBlockCoordinate, chanel);
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
                foreach (var side in blockSides) {
                    int x = entry.Coordinate.x + side.x;
                    int y = entry.Coordinate.y + side.y;
                    int z = entry.Coordinate.z + side.z;
                    var blockCoordinate = new Vector3Int(x, y, z);
                    var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                    var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                    if (world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                        byte level = chunkData.LightMap.Get(localBlockCoordinate, chanel);
						var blockType = chunkData.BlockMap[localBlockCoordinate];
						if (level != 0 && level == entry.Level - BlockDataManager.Data[blockType].Absorption - 1) {
                            var removeEntry = new Entry(x, y, z, level);
                            removeQueue.Enqueue(removeEntry);
                            chunkData.LightMap.Set(localBlockCoordinate, chanel, LightMap.MIN);
                            chunkData.MarkDirty();
                            world.ValidateChunkData(chunkCoordinate, localBlockCoordinate);
                        } else if (level >= entry.Level) {
                            var addEntry = new Entry(x, y, z, level);
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
                    var blockCoordinate = new Vector3Int(x, y, z);
                    var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                    var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                    if (world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                        var blockType = chunkData.BlockMap[localBlockCoordinate];
                        byte level = chunkData.LightMap.Get(localBlockCoordinate, chanel);
                        if (BlockDataManager.Data[blockType].IsTransparent && level + 2 <= entry.Level) {
                            byte newLevel = (byte)(entry.Level - BlockDataManager.Data[blockType].Absorption - 1);
                            chunkData.LightMap.Set(localBlockCoordinate, chanel, newLevel);
                            var addEntry = new Entry(x, y, z, newLevel);
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