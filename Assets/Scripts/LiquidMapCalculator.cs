using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
    public class LiquidMapCalculator
    {
        public struct LiquidData
        {
            public int X, Y, Z;
            public byte Amount;

            public LiquidData(int x, int y, int z, byte amount)
            {
                X = x;
                Y = y;
                Z = z;
                Amount = amount;
            }

            public LiquidData(Vector3Int coordinate, byte amount)
            {
                X = coordinate.x;
                Y = coordinate.y;
                Z = coordinate.z;
                Amount = amount;
            }

        }

        private readonly World world;
        private readonly VoxelType liquidType;
        private readonly Queue<LiquidData> removeQueue = new();
        private readonly Queue<LiquidData> addQueue = new();

        private static readonly Vector3Int[] sides =
        {
            new Vector3Int( 0,  0,  1),
            new Vector3Int( 0,  0, -1),
            new Vector3Int( 0,  1,  0),
            new Vector3Int( 0, -1,  0),
            new Vector3Int( 1,  0,  0),
            new Vector3Int(-1,  0,  0)
        };

        private static readonly Vector3Int[] flowSides =
        {
            new Vector3Int( 0,  0,  1),
            new Vector3Int( 0,  0, -1),
            new Vector3Int( 1,  0,  0),
            new Vector3Int(-1,  0,  0)
        };

        public LiquidMapCalculator(World world, VoxelType liquidType)
        {
            this.world = world;
            this.liquidType = liquidType;
        }

        public void Add(int x, int y, int z, byte amount)
        {
            if (amount < 1)
                return;

            Vector3Int globalVoxelCoordinate = new(x, y, z);
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
            if (!world.ChunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, globalVoxelCoordinate);
            chunkData.LiquidMap[localVoxelCoordinate] = new Liquid(liquidType, amount);
            chunkData.VoxelMap[localVoxelCoordinate] = liquidType;
            chunkData.IsDirty = true;

            LiquidData liquidData = new(x, y, z, amount);
            addQueue.Enqueue(liquidData);
        }

        public void Add(Vector3Int vector, byte amount)
        {
            Add(vector.x, vector.y, vector.z, amount);
        }

        public void Add(int x, int y, int z)
        {
            Vector3Int globalVoxelCoordinate = new(x, y, z);
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
            if (!world.ChunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            chunkData.IsDirty = true;

            Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, globalVoxelCoordinate);
            byte amount = chunkData.LiquidMap.Get(localVoxelCoordinate, liquidType);
            if (amount < 1)
                return;

            chunkData.VoxelMap[localVoxelCoordinate] = liquidType;

            LiquidData liquidData = new(x, y, z, amount);
            addQueue.Enqueue(liquidData);
        }

        public void Add(Vector3Int vector)
        {
            Add(vector.x, vector.y, vector.z);
        }

        public void Remove(int x, int y, int z)
        {
            Vector3Int globalVoxelCoordinate = new(x, y, z);
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
            if (!world.ChunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, globalVoxelCoordinate);
            byte amount = chunkData.LiquidMap.Get(localVoxelCoordinate, liquidType);
            if (amount < 1)
                return;

            chunkData.LiquidMap[localVoxelCoordinate] = Liquid.Empty;
            chunkData.VoxelMap[localVoxelCoordinate] = VoxelType.Air;
            chunkData.IsDirty = true;

            LiquidData liquidData = new(x, y, z, amount);
            removeQueue.Enqueue(liquidData);
        }

        public void Remove(Vector3Int vector)
        {
            Remove(vector.x, vector.y, vector.z);
        }

        public void Calculate()
        {
            var toRemove = new Queue<LiquidData>();
            var toAdd = new Queue<LiquidData>();

            while (removeQueue.TryDequeue(out LiquidData entryLiquidData))
            {
                foreach (var side in sides)
                {
                    int x = entryLiquidData.X + side.x;
                    int y = entryLiquidData.Y + side.y;
                    int z = entryLiquidData.Z + side.z;
                    Vector3Int globalVoxelCoordinate = new(x, y, z);
                    Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
                    if (world.ChunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                    {
                        Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, globalVoxelCoordinate);
                        chunkData.IsDirty = true;
                        byte amount = chunkData.LiquidMap.Get(localVoxelCoordinate, liquidType);
                        if (amount != 0 && (amount == entryLiquidData.Amount - 1 || (side.y == -1 && entryLiquidData.Amount == LiquidMap.MAX)))
                        {
                            LiquidData removeLiquidData = new(x, y, z, amount);
                            toRemove.Enqueue(removeLiquidData);
                            chunkData.LiquidMap.Set(localVoxelCoordinate, liquidType, LiquidMap.MIN);
                            chunkData.VoxelMap[localVoxelCoordinate] = VoxelType.Air;
                        }
                        else if (amount >= entryLiquidData.Amount)
                        {
                            LiquidData addLiquidData = new(x, y, z, amount);
                            toAdd.Enqueue(addLiquidData);
                        }
                    }
                }
            }

            foreach (var item in toAdd)
                addQueue.Enqueue(item);

            while (addQueue.TryDequeue(out LiquidData entryLiquidData))
            {
                int x, y, z;
                Vector3Int globalVoxelCoordinate;
                Vector3Int chunkCoordinate;

                if (entryLiquidData.Amount < 1)
                    continue;

                x = entryLiquidData.X;
                y = entryLiquidData.Y - 1;
                z = entryLiquidData.Z;
                globalVoxelCoordinate = new(x, y, z);
                chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
                if (world.ChunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                {
                    Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, globalVoxelCoordinate);
                    if (world.Voxels[chunkData.VoxelMap[localVoxelCoordinate]].IsSolid)
                    {
                        foreach (var side in flowSides)
                        {
                            x = entryLiquidData.X + side.x;
                            y = entryLiquidData.Y + side.y;
                            z = entryLiquidData.Z + side.z;
                            globalVoxelCoordinate = new(x, y, z);
                            chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
                            if (world.ChunkDatas.TryGetValue(chunkCoordinate, out chunkData))
                            {
                                localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, globalVoxelCoordinate);
                                chunkData.IsDirty = true;
                                VoxelType voxelType = chunkData.VoxelMap[localVoxelCoordinate];
                                byte amount = chunkData.LiquidMap.Get(localVoxelCoordinate, liquidType);
                                if (!world.Voxels[voxelType].IsSolid && amount + 2 <= entryLiquidData.Amount)
                                {
                                    chunkData.LiquidMap.Set(localVoxelCoordinate, liquidType, (byte)(entryLiquidData.Amount - 1));
                                    chunkData.VoxelMap[localVoxelCoordinate] = liquidType;
                                    LiquidData addLiquidData = new(x, y, z, (byte)(entryLiquidData.Amount - 1));
                                    toAdd.Enqueue(addLiquidData);
                                }
                            }
                        }
                    }
                    else
                    {
                        chunkData.IsDirty = true;
                        chunkData.LiquidMap.Set(localVoxelCoordinate, liquidType, LiquidMap.MAX);
                        chunkData.VoxelMap[localVoxelCoordinate] = liquidType;
                        LiquidData addLiquidData = new(x, y, z, LiquidMap.MAX);
                        toAdd.Enqueue(addLiquidData);
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