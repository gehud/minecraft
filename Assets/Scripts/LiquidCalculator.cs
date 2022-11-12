using Minecraft.Utilities;
using System.Collections.Generic;
using UnityEngine;

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
            chunkData.LiquidMap[localBlockCoordinate] = new LiquidData(liquidType, amount);
            chunkData.BlockMap[localBlockCoordinate] = liquidType;
            chunkData.IsDirty = true;

            var entry = new Entry(blockCoordinate, amount);
            addQueue.Enqueue(entry);
        }

        public void Add(Vector3Int blockCoordinate) {
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
            if (!world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            chunkData.IsDirty = true;

            Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
            byte amount = chunkData.LiquidMap.Get(localBlockCoordinate, liquidType);
            if (amount < 1)
                return;

            chunkData.BlockMap[localBlockCoordinate] = liquidType;

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
            chunkData.IsDirty = true;

            Entry entry = new(blockCoordinate, amount);
            removeQueue.Enqueue(entry);
        }

        private Vector3Int GetFlowDirection(Vector3Int origin) {
            var blockManager = BlockDataManager.Instance;
            foreach (var side in flowSides)
                for (int e = 1; e <= 5; e++) {
                    var x = origin.x + side.x * e;
                    var y = origin.y;
                    var z = origin.z + side.z * e;
                    var blockCoordinate = new Vector3Int(x, y, z);
                    var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                    if (world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                        var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                        if (blockManager.Data[chunkData.BlockMap[localBlockCoordinate]].IsSolid)
                            break;
                        chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate + Vector3Int.down);
                        if (world.ChunksData.TryGetValue(chunkCoordinate, out chunkData)) {
                            localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate + Vector3Int.down);
                            if (chunkData.BlockMap[localBlockCoordinate] == BlockType.Air)
                                return side;
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
            var blockManager = BlockDataManager.Instance;

            var toRemove = new Queue<Entry>();
            var toAdd = new Queue<Entry>();

            while (removeQueue.TryDequeue(out Entry entry)) {
                foreach (var side in blockSides) {
                    var x = entry.Coordinate.x + side.x;
                    var y = entry.Coordinate.y + side.y;
                    var z = entry.Coordinate.z + side.z;
                    var blockCoordinate = new Vector3Int(x, y, z);
                    var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                    if (world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                        var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                        chunkData.IsDirty = true;
                        var amount = chunkData.LiquidMap.Get(localBlockCoordinate, liquidType);
                        if (amount != 0 && (amount == entry.Amount - 1
                            || (side.y == -1 && amount == LiquidMap.MAX)) && !IsRenewable(blockCoordinate)) {
                            var removeEntry = new Entry(x, y, z, amount);
                            toRemove.Enqueue(removeEntry);
                            chunkData.LiquidMap.Set(localBlockCoordinate, liquidType, LiquidMap.MIN);
                            chunkData.BlockMap[localBlockCoordinate] = BlockType.Air;
                        } else if (amount >= entry.Amount) {
                            var addEntry = new Entry(x, y, z, amount);
                            toAdd.Enqueue(addEntry);
                        }
                    }
                }
            }

            foreach (var item in toAdd)
                addQueue.Enqueue(item);

            while (addQueue.TryDequeue(out Entry entry)) {
                int x, y, z;
                Vector3Int blockCoordinate;
                Vector3Int chunkCoordinate;

                if (entry.Amount < 1)
                    continue;

                x = entry.Coordinate.x;
                y = entry.Coordinate.y - 1;
                z = entry.Coordinate.z;
                blockCoordinate = new(x, y, z);
                chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                if (world.ChunksData.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                    Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                    if (blockManager.Data[chunkData.BlockMap[localBlockCoordinate]].IsSolid) {
                        Vector3Int flowDirection = GetFlowDirection(entry.Coordinate);
                        foreach (var side in flowSides) {
                            if (flowDirection != Vector3Int.zero && side != flowDirection)
                                continue;

                            x = entry.Coordinate.x + side.x;
                            y = entry.Coordinate.y + side.y;
                            z = entry.Coordinate.z + side.z;
                            blockCoordinate = new(x, y, z);
                            chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
                            if (world.ChunksData.TryGetValue(chunkCoordinate, out chunkData)) {
                                localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
                                chunkData.IsDirty = true;
                                var blockType = chunkData.BlockMap[localBlockCoordinate];
                                var amount = chunkData.LiquidMap.Get(localBlockCoordinate, liquidType);
                                if (!blockManager.Data[blockType].IsSolid) {
                                    if (amount + 2 <= entry.Amount) {
                                        chunkData.LiquidMap.Set(localBlockCoordinate, liquidType, (byte)(entry.Amount - 1));
                                        chunkData.BlockMap[localBlockCoordinate] = liquidType;
                                        var addEntry = new Entry(x, y, z, (byte)(entry.Amount - 1));
                                        toAdd.Enqueue(addEntry);
                                    } else if (entry.Amount == LiquidMap.MAX && amount == entry.Amount - 1) {
                                        chunkData.LiquidMap.Set(localBlockCoordinate, liquidType, entry.Amount);
                                        chunkData.BlockMap[localBlockCoordinate] = liquidType;
                                        var addEntry = new Entry(x, y, z, entry.Amount);
                                        toAdd.Enqueue(addEntry);
                                    }
                                }
                            }
                        }
                    } else {
                        chunkData.IsDirty = true;
                        chunkData.LiquidMap.Set(localBlockCoordinate, liquidType, LiquidMap.MAX);
                        chunkData.BlockMap[localBlockCoordinate] = liquidType;
                        var addEntry = new Entry(x, y, z, LiquidMap.MAX);
                        toAdd.Enqueue(addEntry);
                    }
                }
            }

            foreach (var item in toRemove)
                removeQueue.Enqueue(item);
            foreach (var item in toAdd)
                addQueue.Enqueue(item);
        }
    }
}