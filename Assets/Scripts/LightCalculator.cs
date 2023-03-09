using Minecraft.Utilities;
using System.Collections.Concurrent;
using UnityEngine;

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

		private static BlockProvider BlockDataProvider { get; set; }

        private static readonly Vector3Int[] blockSides = {
            new Vector3Int( 0,  0,  1),
            new Vector3Int( 0,  0, -1),
            new Vector3Int( 0,  1,  0),
            new Vector3Int( 0, -1,  0),
            new Vector3Int( 1,  0,  0),
            new Vector3Int(-1,  0,  0),
        };

        public static void SetBlockDataManager(BlockProvider blockDataManager) {
            BlockDataProvider = blockDataManager;
        }

        public LightCalculator(World world, LightChanel chanel) {
            this.world = world;
            this.chanel = chanel;
        }

        public void Add(Vector3Int blockCoordinate, byte level) {
            if (level <= 1)
                return;

            var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.TryGetChunk(chunkCoordinate, out Chunk chunk))
                return;

            var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            chunk.LightMap.Set(localBlockCoordinate, chanel, level);
            chunk.MarkDirty();
            world.ValidateChunk(chunkCoordinate, localBlockCoordinate);

            var entry = new Entry(blockCoordinate, level);
            addQueue.Enqueue(entry);
        }

        public void Add(int x, int y, int z, byte level) {
            Add(new Vector3Int(x, y, z), level);
        }

        public void Add(Vector3Int blockCoordinate) {
            var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.TryGetChunk(chunkCoordinate, out Chunk chunk))
                return;

            var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            var level = chunk.LightMap.Get(localBlockCoordinate, chanel);
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
            if (!world.TryGetChunk(chunkCoordinate, out Chunk chunk))
                return;

            var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            byte level = chunk.LightMap.Get(localBlockCoordinate, chanel);
            if (level <= 1)
                return;

            chunk.LightMap.Set(localBlockCoordinate, chanel, LightMap.MIN);
            world.ValidateChunk(chunkCoordinate, localBlockCoordinate);
            chunk.MarkDirty();

            var entry = new Entry(blockCoordinate, level);
            removeQueue.Enqueue(entry);
        }

        public void Remove(int x, int y, int z) {
            Remove(new Vector3Int(x, y, z));
        }

        public static void AddSunlight(World world, Vector2Int column) {
            int startX = column.x * Chunk.SIZE;
            int endX = column.x * Chunk.SIZE + Chunk.SIZE;
            int startZ = column.y * Chunk.SIZE;
            int endZ = column.y * Chunk.SIZE + Chunk.SIZE;
            for (int x = startX; x < endX; x++)
                for (int z = startZ; z < endZ; z++) {
                    for (int y = World.HEIGHT * Chunk.SIZE - 1; y >= 0; y--) {
                        var blockCoordinate = new Vector3Int(x, y, z);
                        var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                        if (!world.TryGetChunk(chunkCoordinate, out Chunk chunk))
                            break;
                        var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                        if (chunk.BlockMap[localBlockCoordinate] != BlockType.Air)
                            break;
                        world.LightCalculatorSun.Add(blockCoordinate, LightMap.MAX);
                    }
                }
        }

        public void Calculate() {
            while (removeQueue.TryDequeue(out Entry entry)) {
				for (int i = 0; i < blockSides.Length; i++) {
                    var blockCoordinate = entry.Coordinate + blockSides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                    if (world.TryGetChunk(chunkCoordinate, out Chunk chunk)) {
                        var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                        var level = chunk.LightMap.Get(localBlockCoordinate, chanel);
                        var blockType = chunk.BlockMap[localBlockCoordinate];
                        var absorption = BlockDataProvider.Get(blockType).Absorption;
                        if (level != 0 && level == entry.Level - absorption - 1) {
                            var removeEntry = new Entry(blockCoordinate, level);
                            removeQueue.Enqueue(removeEntry);
                            chunk.LightMap.Set(localBlockCoordinate, chanel, LightMap.MIN);
                            chunk.MarkDirty();
                            world.ValidateChunk(chunkCoordinate, localBlockCoordinate);
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
                    if (world.TryGetChunk(chunkCoordinate, out Chunk chunk)) {
                        var blockType = chunk.BlockMap[localBlockCoordinate];
                        var absorption = BlockDataProvider.Get(blockType).Absorption;
                        var level = chunk.LightMap.Get(localBlockCoordinate, chanel);
                        if (BlockDataProvider.Get(blockType).IsTransparent && level + absorption + 1 < entry.Level) {
                            var newLevel = (byte)(entry.Level - absorption - 1);
                            chunk.LightMap.Set(localBlockCoordinate, chanel, newLevel);
                            var addEntry = new Entry(blockCoordinate, newLevel);
                            addQueue.Enqueue(addEntry);
                            chunk.MarkDirty();
                            world.ValidateChunk(chunkCoordinate, localBlockCoordinate);
                        }
                    }
                }
            }
        }
    }
}