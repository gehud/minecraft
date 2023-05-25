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

        private readonly int chanel;
        private readonly World world;
        private readonly BlockProvider blockProvider;
        private readonly ConcurrentQueue<Entry> addQueue = new();
        private readonly ConcurrentQueue<Entry> removeQueue = new();

        private static readonly Vector3Int[] blockSides = {
            new Vector3Int( 0,  0,  1),
            new Vector3Int( 0,  0, -1),
            new Vector3Int( 0,  1,  0),
            new Vector3Int( 0, -1,  0),
            new Vector3Int( 1,  0,  0),
            new Vector3Int(-1,  0,  0),
        };

        public LightCalculator(int chanel, World world, BlockProvider blockProvider) {
            this.world = world;
            this.chanel = chanel;
            this.blockProvider = blockProvider;
        }

        public void AddLight(Vector3Int blockCoordinate, byte level) {
            if (level <= 1)
                return;

            var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.TryGetChunk(chunkCoordinate, out Chunk chunk))
                return;

            var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            chunk.LightMap.Set(localBlockCoordinate, chanel, level);
            chunk.IsDirty = true;
            world.MarkDirtyIfNeeded(chunkCoordinate, localBlockCoordinate);
			if (chunk.IsSaved) {
				world.MarkModifiedIfNeeded(chunkCoordinate, localBlockCoordinate);
			}

			var entry = new Entry(blockCoordinate, level);
            addQueue.Enqueue(entry);
        }

        public void AddLight(int x, int y, int z, byte level) {
            AddLight(new Vector3Int(x, y, z), level);
        }

        public void AddLight(Vector3Int blockCoordinate) {
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

        public void AddLight(int x, int y, int z) {
            AddLight(new Vector3Int(x, y, z));
        }

        public void RemoveLight(Vector3Int blockCoordinate) {
            var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.TryGetChunk(chunkCoordinate, out Chunk chunk))
                return;

            var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            byte level = chunk.LightMap.Get(localBlockCoordinate, chanel);
            if (level <= 1)
                return;

            chunk.LightMap.Set(localBlockCoordinate, chanel, LightMap.MIN);
            chunk.IsDirty = true;
            world.MarkDirtyIfNeeded(chunkCoordinate, localBlockCoordinate);
			if (chunk.IsSaved) {
				world.MarkModifiedIfNeeded(chunkCoordinate, localBlockCoordinate);
			}

			var entry = new Entry(blockCoordinate, level);
            removeQueue.Enqueue(entry);
        }

        public void RemoveLight(int x, int y, int z) {
            RemoveLight(new Vector3Int(x, y, z));
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
                        var absorption = blockProvider.Get(blockType).Absorption;
                        if (level != 0 && level == entry.Level - absorption - 1) {
                            var removeEntry = new Entry(blockCoordinate, level);
                            removeQueue.Enqueue(removeEntry);
                            chunk.LightMap.Set(localBlockCoordinate, chanel, LightMap.MIN);
                            chunk.IsDirty = true;
                            world.MarkDirtyIfNeeded(chunkCoordinate, localBlockCoordinate);
							if (chunk.IsSaved) {
								world.MarkModifiedIfNeeded(chunkCoordinate, localBlockCoordinate);
							}
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
                        var absorption = blockProvider.Get(blockType).Absorption;
                        var level = chunk.LightMap.Get(localBlockCoordinate, chanel);
                        if (blockProvider.Get(blockType).IsTransparent && level + absorption + 1 < entry.Level) {
                            var newLevel = (byte)(entry.Level - absorption - 1);
                            chunk.LightMap.Set(localBlockCoordinate, chanel, newLevel);
                            var addEntry = new Entry(blockCoordinate, newLevel);
                            addQueue.Enqueue(addEntry);
                            chunk.IsDirty = true;
                            world.MarkDirtyIfNeeded(chunkCoordinate, localBlockCoordinate);
							if (chunk.IsSaved) {
								world.MarkModifiedIfNeeded(chunkCoordinate, localBlockCoordinate);
                            }
						}
                    }
                }
            }
        }
    }
}