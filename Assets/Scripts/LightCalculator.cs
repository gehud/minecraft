using System.Collections.Generic;
using UnityEngine;

namespace Minecraft {
    public class LightCalculator {
        public struct LightData {
            public int X, Y, Z;
            public byte Light;

            public LightData(int x, int y, int z, byte light) {
                X = x;
                Y = y;
                Z = z;
                Light = light;
            }

            public LightData(Vector3Int coordinate, byte light) {
                X = coordinate.x;
                Y = coordinate.y;
                Z = coordinate.z;
                Light = light;
            }
        }

        private World world;
        private LightChanel chanel;
        private Queue<LightData> addQueue = new();
        private Queue<LightData> removeQueue = new();

        public LightCalculator(World world, LightChanel chanel) {
            this.world = world;
            this.chanel = chanel;
        }

        public void Add(int x, int y, int z, int light) {
            if (light <= 1)
                return;

            Vector3Int globalVoxelCoordinate = new(x, y, z);
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
            if (!world.ChunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, globalVoxelCoordinate);
            chunkData.LightMap.Set(localVoxelCoordinate, chanel, light);
            chunkData.IsDirty = true;

            LightData lightData = new(x, y, z, (byte)light);
            addQueue.Enqueue(lightData);
        }

        public void Add(Vector3Int vector, int light) {
            Add(vector.x, vector.y, vector.z, light);
        }

        public void Add(int x, int y, int z) {
            Vector3Int globalVoxelCoordinate = new(x, y, z);
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
            if (!world.ChunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            chunkData.IsDirty = true;

            Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, globalVoxelCoordinate);
            int light = chunkData.LightMap.Get(localVoxelCoordinate, chanel);
            if (light <= 1)
                return;

            LightData lightData = new(x, y, z, (byte)light);
            addQueue.Enqueue(lightData);
        }

        public void Add(Vector3Int vector) {
            Add(vector.x, vector.y, vector.z);
        }

        public void Remove(int x, int y, int z) {
            Vector3Int globalVoxelCoordinate = new(x, y, z);
            Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
            if (!world.ChunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                return;

            Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, globalVoxelCoordinate);
            int light = chunkData.LightMap.Get(localVoxelCoordinate, chanel);
            if (light <= 1)
                return;

            chunkData.LightMap.Set(localVoxelCoordinate, chanel, LightMap.MIN);
            chunkData.IsDirty = true;

            LightData lightData = new(x, y, z, (byte)light);
            removeQueue.Enqueue(lightData);
        }

        public void Remove(Vector3Int vector) {
            Remove(vector.x, vector.y, vector.z);
        }

        public static void AddSunlight(World world, Vector2Int column) {
            var blockDataManager = BlockManager.Instance;

            int startX = column.x * Chunk.SIZE;
            int endX = column.x * Chunk.SIZE + Chunk.SIZE - 1;
            int startZ = column.y * Chunk.SIZE;
            int endZ = column.y * Chunk.SIZE + Chunk.SIZE - 1;

            // Add sunlight.
            for (int x = startX; x <= endX; x++)
                for (int z = startZ; z <= endZ; z++) {
                    for (int y = World.HEIGHT * Chunk.SIZE - 1; y >= 0; y--) {
                        Vector3Int globalVoxelCoordinate = new(x, y, z);
                        Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
                        if (!world.ChunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData))
                            break;
                        Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, globalVoxelCoordinate);
                        if (!blockDataManager.Blocks[chunkData.BlockMap[localVoxelCoordinate]].IsTransparent)
                            break;
                        chunkData.LightMap.SetSun(localVoxelCoordinate, LightMap.MAX);
                    }
                }

            // Receive sunlight.
            for (int x = startX; x <= endX; x++)
                for (int z = startZ; z <= endZ; z++) {
                    for (int y = World.HEIGHT * Chunk.SIZE - 1; y >= 0; y--) {
                        Vector3Int globalVoxelCoordinate = new(x, y, z);
                        Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
                        if (!world.ChunkDatas.ContainsKey(chunkCoordinate))
                            break;
                        if (!blockDataManager.Blocks[world.GetVoxel(globalVoxelCoordinate)].IsTransparent) {
                            for (int newY = y - 1; newY >= 0; newY--) {
                                globalVoxelCoordinate = new Vector3Int(x, newY, z);
                                chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
                                if (!blockDataManager.Blocks[world.GetVoxel(globalVoxelCoordinate)].IsTransparent)
                                    continue;
                                if (world.ChunkDatas.ContainsKey(chunkCoordinate + Vector3Int.right)
                                    && world.GetLight(globalVoxelCoordinate + Vector3Int.right, LightChanel.Sun) == LightMap.MAX)
                                    world.LightCalculatorSun.Add(globalVoxelCoordinate + Vector3Int.right);
                                if (world.ChunkDatas.ContainsKey(chunkCoordinate + Vector3Int.left)
                                    && world.GetLight(globalVoxelCoordinate + Vector3Int.left, LightChanel.Sun) == LightMap.MAX)
                                    world.LightCalculatorSun.Add(globalVoxelCoordinate + Vector3Int.left);
                                if (world.ChunkDatas.ContainsKey(chunkCoordinate + Vector3Int.forward)
                                    && world.GetLight(globalVoxelCoordinate + Vector3Int.forward, LightChanel.Sun) == LightMap.MAX)
                                    world.LightCalculatorSun.Add(globalVoxelCoordinate + Vector3Int.forward);
                                if (world.ChunkDatas.ContainsKey(chunkCoordinate + Vector3Int.back)
                                    && world.GetLight(globalVoxelCoordinate + Vector3Int.back, LightChanel.Sun) == LightMap.MAX)
                                    world.LightCalculatorSun.Add(globalVoxelCoordinate + Vector3Int.back);
                            }
                            break;
                        }
                    }
                }
        }

        public void Calculate() {
            Vector3Int[] sides =
            {
                 new Vector3Int( 0,  0,  1),
                 new Vector3Int( 0,  0, -1),
                 new Vector3Int( 0,  1,  0),
                 new Vector3Int( 0, -1,  0),
                 new Vector3Int( 1,  0,  0),
                 new Vector3Int(-1,  0,  0)
            };

            while (removeQueue.TryDequeue(out LightData entryLightData)) {
                foreach (var side in sides) {
                    int x = entryLightData.X + side.x;
                    int y = entryLightData.Y + side.y;
                    int z = entryLightData.Z + side.z;
                    Vector3Int globalVoxelCoordinate = new(x, y, z);
                    Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
                    Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, globalVoxelCoordinate);
                    if (world.ChunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                        chunkData.IsDirty = true;
                        int light = chunkData.LightMap.Get(localVoxelCoordinate, chanel);
                        if (light != 0 && light == entryLightData.Light - 1) {
                            LightData removeLightData = new(x, y, z, (byte)light);
                            removeQueue.Enqueue(removeLightData);
                            chunkData.LightMap.Set(localVoxelCoordinate, chanel, LightMap.MIN);
                        } else if (light >= entryLightData.Light) {
                            LightData addLightData = new(x, y, z, (byte)light);
                            addQueue.Enqueue(addLightData);
                        }
                    }
                }
            }

            while (addQueue.TryDequeue(out LightData entryLightData)) {
                if (entryLightData.Light <= 1)
                    continue;

                foreach (var side in sides) {
                    int x = entryLightData.X + side.x;
                    int y = entryLightData.Y + side.y;
                    int z = entryLightData.Z + side.z;
                    Vector3Int globalVoxelCoordinate = new(x, y, z);
                    Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(globalVoxelCoordinate);
                    Vector3Int localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, globalVoxelCoordinate);
                    if (world.ChunkDatas.TryGetValue(chunkCoordinate, out ChunkData chunkData)) {
                        chunkData.IsDirty = true;
                        BlockType voxelType = chunkData.BlockMap[localVoxelCoordinate];
                        int light = chunkData.LightMap.Get(localVoxelCoordinate, chanel);
                        if (voxelType == BlockType.Air && light + 2 <= entryLightData.Light) {
                            chunkData.LightMap.Set(localVoxelCoordinate, chanel, (byte)(entryLightData.Light - 1));
                            LightData addLightData = new(x, y, z, (byte)(entryLightData.Light - 1));
                            addQueue.Enqueue(addLightData);
                        }
                    }
                }
            }
        }
    }
}