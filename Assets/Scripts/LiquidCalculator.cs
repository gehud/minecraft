using Minecraft.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Minecraft {
    public class LiquidCalculator {
        private struct Entry {
            public Vector3Int Coordinate;
            public byte Amount;

            public Entry(Vector3Int coordinate, byte amount) {
                Coordinate = coordinate;
                Amount = amount;
            }

            public Entry(int x, int y, int z, byte amount) : this(new Vector3Int(x, y, z), amount) { }
        }

        private readonly World world;
        private readonly BlockType liquidType;
        private readonly Queue<Entry> removeQueue = new();
        private readonly Queue<Entry> addQueue = new();

        private static BlockDataManager BlockDataManager { get; set; }

        private static readonly Vector3Int[] blockSides = {
            new Vector3Int( 0,  0,  1),
            new Vector3Int( 0,  0, -1),
            new Vector3Int( 0,  1,  0),
            new Vector3Int( 0, -1,  0),
            new Vector3Int( 1,  0,  0),
            new Vector3Int(-1,  0,  0),
        };

        private static readonly Vector3Int[] flowSides = {
            new Vector3Int( 0,  0,  1),
            new Vector3Int( 0,  0, -1),
            new Vector3Int( 1,  0,  0),
            new Vector3Int(-1,  0,  0),
        };

        public static void SetBlockDataManager(BlockDataManager blockDataManager) {
            BlockDataManager = blockDataManager;
        }

        public LiquidCalculator(World world, BlockType liquidType) {
            this.world = world;
            this.liquidType = liquidType;
        }

        public void Add(Vector3Int blockCoordinate, byte amount) {
            if (amount < 1)
                return;

            var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            if (BlockDataManager.Data[chunkData.BlockMap[localBlockCoordinate]].IsSolid)
                return;

            chunkData.LiquidMap[localBlockCoordinate] = new LiquidData(liquidType, amount);
            chunkData.BlockMap[localBlockCoordinate] = liquidType;
            chunkData.MarkDirty();
            world.ValidateChunkData(chunkCoordinate, localBlockCoordinate);

            var entry = new Entry(blockCoordinate, amount);
            addQueue.Enqueue(entry);
        }

        public void Add(Vector3Int blockCoordinate) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            byte amount = chunkData.LiquidMap.Get(localBlockCoordinate, liquidType);
            if (amount < 1)
                return;

            var entry = new Entry(blockCoordinate, amount);
            addQueue.Enqueue(entry);
        }

        public void Remove(Vector3Int blockCoordinate) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            byte amount = chunkData.LiquidMap.Get(localBlockCoordinate, liquidType);
            if (amount < 1)
                return;

            chunkData.LiquidMap[localBlockCoordinate] = LiquidData.Empty;
            chunkData.BlockMap[localBlockCoordinate] = BlockType.Air;
            chunkData.MarkDirty();
			world.ValidateChunkData(chunkCoordinate, localBlockCoordinate);

			Entry entry = new(blockCoordinate, amount);
            removeQueue.Enqueue(entry);
        }

        private Vector3Int GetFlowDirection(Vector3Int origin) {
            foreach (var side in flowSides) {
                for (int e = 1; e <= 5; e++) {
                    var x = origin.x + side.x * e;
                    var y = origin.y;
                    var z = origin.z + side.z * e;
                    var blockCoordinate = new Vector3Int(x, y, z);
                    var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                    if (world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                        var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                        if (BlockDataManager.Data[chunkData.BlockMap[localBlockCoordinate]].IsSolid)
                            break;
                        blockCoordinate += Vector3Int.down;
                        chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                        if (world.ChunksData.TryGetValue(chunkCoordinate, out chunkData)) {
                            localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                            if (chunkData.BlockMap[localBlockCoordinate] == BlockType.Air)
                                return side;
                        }
                    }
                }
            }

            return Vector3Int.zero;
        }

        private bool IsRenewable(Vector3Int blockCoordinate) {
            var amount000 = world.GetLiquidAmount(blockCoordinate + Vector3Int.right);
            var amount090 = world.GetLiquidAmount(blockCoordinate + Vector3Int.forward);
            var amount180 = world.GetLiquidAmount(blockCoordinate + Vector3Int.left);
            var amount270 = world.GetLiquidAmount(blockCoordinate + Vector3Int.back);
            return (amount000 == LiquidMap.MAX && amount090 == LiquidMap.MAX)
                || (amount000 == LiquidMap.MAX && amount180 == LiquidMap.MAX)
                || (amount000 == LiquidMap.MAX && amount270 == LiquidMap.MAX)
                || (amount090 == LiquidMap.MAX && amount180 == LiquidMap.MAX)
                || (amount090 == LiquidMap.MAX && amount270 == LiquidMap.MAX)
                || (amount180 == LiquidMap.MAX && amount270 == LiquidMap.MAX);
        }

        public void Calculate() {
            Profiler.BeginSample("LiquidCalculator.Calculate");

            var toRemove = new Queue<Entry>();
            var toAdd = new Queue<Entry>();

            while (removeQueue.TryDequeue(out Entry entry)) {
                foreach (var side in blockSides) {
                    var blockCoordinate = entry.Coordinate + side;
                    var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                    if (world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                        var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                        var amount = chunkData.LiquidMap.Get(localBlockCoordinate, liquidType);
                        if (amount != 0
                            && (amount == entry.Amount - 1 || side.y == -1 && amount == LiquidMap.MAX)
                            && !IsRenewable(blockCoordinate)) {
                            var removeEntry = new Entry(blockCoordinate, amount);
                            toRemove.Enqueue(removeEntry);
                            chunkData.LiquidMap.Set(localBlockCoordinate, liquidType, LiquidMap.MIN);
                            chunkData.BlockMap[localBlockCoordinate] = BlockType.Air;
                            chunkData.MarkDirty();
                            world.ValidateChunkData(chunkCoordinate, localBlockCoordinate);
                        } else if (amount >= entry.Amount) {
                            var addEntry = new Entry(blockCoordinate, amount);
                            addQueue.Enqueue(addEntry);
                        }
                    }
                }
            }

			while (addQueue.TryDequeue(out Entry entry)) {
                Vector3Int blockCoordinate;
                Vector3Int chunkCoordinate;

                if (entry.Amount < 1)
                    continue;

                blockCoordinate = entry.Coordinate + Vector3Int.down;
                chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                if (world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                    Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
					if (BlockDataManager.Data[chunkData.BlockMap[localBlockCoordinate]].IsSolid) {
                        Vector3Int flowDirection = GetFlowDirection(entry.Coordinate);
                        foreach (var side in flowSides) {
                            blockCoordinate = entry.Coordinate + side;
                            if (flowDirection != Vector3Int.zero && flowDirection != side 
                                && BlockDataManager.Data[world.GetBlock(blockCoordinate + Vector3Int.down)].IsSolid)
                                continue;
                            chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                            if (world.ChunksData.TryGetValue(chunkCoordinate, out chunkData)) {
                                localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                                var blockType = chunkData.BlockMap[localBlockCoordinate];
                                var amount = chunkData.LiquidMap.Get(localBlockCoordinate, liquidType);
                                if (!BlockDataManager.Data[blockType].IsSolid) {
                                    if (amount + 2 <= entry.Amount) {
                                        byte newAmount = (byte)(entry.Amount - 1);
                                        chunkData.LiquidMap.Set(localBlockCoordinate, liquidType, newAmount);
                                        chunkData.BlockMap[localBlockCoordinate] = liquidType;
                                        var addEntry = new Entry(blockCoordinate, newAmount);
                                        toAdd.Enqueue(addEntry);
										chunkData.MarkDirty();
										world.ValidateChunkData(chunkCoordinate, localBlockCoordinate);
									}
                                }
                            }
                        }
                    } else {
                        chunkData.LiquidMap.Set(localBlockCoordinate, liquidType, LiquidMap.MAX);
                        chunkData.BlockMap[localBlockCoordinate] = liquidType;
                        var addEntry = new Entry(blockCoordinate, LiquidMap.MAX);
                        toAdd.Enqueue(addEntry);
						chunkData.MarkDirty();
						world.ValidateChunkData(chunkCoordinate, localBlockCoordinate);
					}
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
                removeQueue.Enqueue(toRemove.Dequeue());
			for (int i = 0; i < toAdd.Count; i++)
				addQueue.Enqueue(toAdd.Dequeue());

            Profiler.EndSample();
        }
    }
}