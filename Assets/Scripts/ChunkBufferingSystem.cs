using Minecraft.Lighting;
using Minecraft.Player;
using Minecraft.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Minecraft {
    [BurstCompile]
    [UpdateBefore(typeof(ChunkSpawnSystem))]
    public partial struct ChunkBufferingSystem : ISystem {
        public const int BufferDistance = 1;

        [BurstCompile]
        public static int ToIndex(in ChunkBufferingSystemData systemData, in int3 coordinate) {
            var arrayCoordinate = new int3 {
                x = coordinate.x - systemData.Center.x + systemData.DrawDistance + BufferDistance,
                y = coordinate.y,
                z = coordinate.z - systemData.Center.y + systemData.DrawDistance + BufferDistance
            };

            return IndexUtility.ToIndex(arrayCoordinate, systemData.ChunksSize, systemData.Height);
        }

        [BurstCompile]
        private static bool IsOutOfBuffer(in ChunkBufferingSystemData systemData, in int3 arrayCoordinate) {
            return arrayCoordinate.x < 0
                || arrayCoordinate.y < 0
                || arrayCoordinate.z < 0
                || arrayCoordinate.x >= systemData.ChunksSize
                || arrayCoordinate.y >= systemData.Height
                || arrayCoordinate.z >= systemData.ChunksSize;
        }

        [BurstCompile]
        public static void GetEntity(in ChunkBufferingSystemData systemData, in int3 coordinate, out Entity entity) {
            var arrayCoordinate = new int3 {
                x = coordinate.x - systemData.Center.x + systemData.DrawDistance + BufferDistance,
                y = coordinate.y,
                z = coordinate.z - systemData.Center.y + systemData.DrawDistance + BufferDistance
            };

            if (IsOutOfBuffer(systemData, arrayCoordinate)) {
                entity = Entity.Null;
                return;
            }

            var index = IndexUtility.ToIndex(arrayCoordinate, systemData.ChunksSize, systemData.Height);
            entity = systemData.Chunks[index];
        }

        [BurstCompile]
        public static bool TryGetEntity(in ChunkBufferingSystemData systemData, in int3 chunkCoordinate, out Entity entity) {
            GetEntity(systemData, chunkCoordinate, out entity);
            return entity != Entity.Null;
        }

        [BurstCompile]
        public static void GetVoxel(in ChunkBufferingSystemData systemData, in EntityManager entityManager, in int3 coordinate, out Voxel voxel) {
            var chunkCoordinate = new int3 {
                x = (int)math.floor(coordinate.x / (float)Chunk.Size),
                y = (int)math.floor(coordinate.y / (float)Chunk.Size),
                z = (int)math.floor(coordinate.z / (float)Chunk.Size)
            };

            GetEntity(systemData, chunkCoordinate, out var entity);
            if (entity == Entity.Null
                || !entityManager.HasComponent<Chunk>(entity)
                || entityManager.HasComponent<RawChunk>(entity)) {
                voxel = default;
                return;
            }

            var localCoordinate = coordinate - chunkCoordinate * Chunk.Size;
            var index = IndexUtility.ToIndex(localCoordinate, Chunk.Size, Chunk.Size);
            voxel = entityManager.GetComponentData<Chunk>(entity).Voxels[index];
        }

        [BurstCompile]
        private static void MarkDirtyIfExistsImmediate(in ChunkBufferingSystemData systemData, in EntityManager entityManager, in int3 chunkCoordinate) {
            GetEntity(systemData, chunkCoordinate, out var entity);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity)) {
                return;
            }

            entityManager.AddComponent<DirtyChunk>(entity);
            entityManager.AddComponent<ImmediateChunk>(entity);
        }

        [BurstCompile]
        private static void MarkDirtyIfExistsImmediate(in ChunkBufferingSystemData systemData, in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 chunkCoordinate) {
            GetEntity(systemData, chunkCoordinate, out var entity);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity)) {
                return;
            }

            commandBuffer.AddComponent<DirtyChunk>(entity);
            commandBuffer.AddComponent<ImmediateChunk>(entity);
        }

        [BurstCompile]
        private static void MarkDirtyIfExists(in ChunkBufferingSystemData systemData, in EntityManager entityManager, in int3 chunkCoordinate) {
            GetEntity(systemData, chunkCoordinate, out var entity);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity)) {
                return;
            }

            entityManager.AddComponent<DirtyChunk>(entity);
        }

        [BurstCompile]
        private static void MarkDirtyIfExists(in ChunkBufferingSystemData systemData, in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 chunkCoordinate) {
            GetEntity(systemData, chunkCoordinate, out var entity);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity) || entityManager.HasComponent<DirtyChunk>(entity)) {
                return;
            }

            commandBuffer.AddComponent<DirtyChunk>(entity);
        }

        [BurstCompile]
        public static void MarkDirtyIfNeededImmediate(in ChunkBufferingSystemData systemData, in EntityManager entityManager, in int3 chunkCoordinate, in int3 localVoxelCoordinate) {
            if (localVoxelCoordinate.x == 0) {
                MarkDirtyIfExistsImmediate(systemData, entityManager, chunkCoordinate + new int3(-1, 0, 0));
            }

            if (localVoxelCoordinate.y == 0) {
                MarkDirtyIfExistsImmediate(systemData, entityManager, chunkCoordinate + new int3(0, -1, 0));
            }

            if (localVoxelCoordinate.z == 0) {
                MarkDirtyIfExistsImmediate(systemData, entityManager, chunkCoordinate + new int3(0, 0, -1));
            }

            if (localVoxelCoordinate.x == Chunk.Size - 1) {
                MarkDirtyIfExistsImmediate(systemData, entityManager, chunkCoordinate + new int3(1, 0, 0));
            }

            if (localVoxelCoordinate.y == Chunk.Size - 1) {
                MarkDirtyIfExistsImmediate(systemData, entityManager, chunkCoordinate + new int3(0, 1, 0));
            }

            if (localVoxelCoordinate.z == Chunk.Size - 1) {
                MarkDirtyIfExistsImmediate(systemData, entityManager, chunkCoordinate + new int3(0, 0, 1));
            }
        }

        [BurstCompile]
        public static void MarkDirtyIfNeededImmediate(in ChunkBufferingSystemData systemData, in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 chunkCoordinate, in int3 localVoxelCoordinate) {
            if (localVoxelCoordinate.x == 0) {
                MarkDirtyIfExistsImmediate(systemData, entityManager, commandBuffer, chunkCoordinate + new int3(-1, 0, 0));
            }

            if (localVoxelCoordinate.y == 0) {
                MarkDirtyIfExistsImmediate(systemData, entityManager, commandBuffer, chunkCoordinate + new int3(0, -1, 0));
            }

            if (localVoxelCoordinate.z == 0) {
                MarkDirtyIfExistsImmediate(systemData, entityManager, commandBuffer, chunkCoordinate + new int3(0, 0, -1));
            }

            if (localVoxelCoordinate.x == Chunk.Size - 1) {
                MarkDirtyIfExistsImmediate(systemData, entityManager, commandBuffer, chunkCoordinate + new int3(1, 0, 0));
            }

            if (localVoxelCoordinate.y == Chunk.Size - 1) {
                MarkDirtyIfExistsImmediate(systemData, entityManager, commandBuffer, chunkCoordinate + new int3(0, 1, 0));
            }

            if (localVoxelCoordinate.z == Chunk.Size - 1) {
                MarkDirtyIfExistsImmediate(systemData, entityManager, commandBuffer, chunkCoordinate + new int3(0, 0, 1));
            }
        }

        [BurstCompile]
        public static void MarkDirtyIfNeeded(in ChunkBufferingSystemData systemData, in EntityManager entityManager, in int3 chunkCoordinate, in int3 localVoxelCoordinate) {
            if (localVoxelCoordinate.x == 0) {
                MarkDirtyIfExists(systemData, entityManager, chunkCoordinate + new int3(-1, 0, 0));
            }

            if (localVoxelCoordinate.y == 0) {
                MarkDirtyIfExists(systemData, entityManager, chunkCoordinate + new int3(0, -1, 0));
            }

            if (localVoxelCoordinate.z == 0) {
                MarkDirtyIfExists(systemData, entityManager, chunkCoordinate + new int3(0, 0, -1));
            }

            if (localVoxelCoordinate.x == Chunk.Size - 1) {
                MarkDirtyIfExists(systemData, entityManager, chunkCoordinate + new int3(1, 0, 0));
            }

            if (localVoxelCoordinate.y == Chunk.Size - 1) {
                MarkDirtyIfExists(systemData, entityManager, chunkCoordinate + new int3(0, 1, 0));
            }

            if (localVoxelCoordinate.z == Chunk.Size - 1) {
                MarkDirtyIfExists(systemData, entityManager, chunkCoordinate + new int3(0, 0, 1));
            }
        }

        [BurstCompile]
        public static void MarkDirtyIfNeeded(in ChunkBufferingSystemData systemData, in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 chunkCoordinate, in int3 localVoxelCoordinate) {
            if (localVoxelCoordinate.x == 0) {
                MarkDirtyIfExists(systemData, entityManager, commandBuffer, chunkCoordinate + new int3(-1, 0, 0));
            }

            if (localVoxelCoordinate.y == 0) {
                MarkDirtyIfExists(systemData, entityManager, commandBuffer, chunkCoordinate + new int3(0, -1, 0));
            }

            if (localVoxelCoordinate.z == 0) {
                MarkDirtyIfExists(systemData, entityManager, commandBuffer, chunkCoordinate + new int3(0, 0, -1));
            }

            if (localVoxelCoordinate.x == Chunk.Size - 1) {
                MarkDirtyIfExists(systemData, entityManager, commandBuffer, chunkCoordinate + new int3(1, 0, 0));
            }

            if (localVoxelCoordinate.y == Chunk.Size - 1) {
                MarkDirtyIfExists(systemData, entityManager, commandBuffer, chunkCoordinate + new int3(0, 1, 0));
            }

            if (localVoxelCoordinate.z == Chunk.Size - 1) {
                MarkDirtyIfExists(systemData, entityManager, commandBuffer, chunkCoordinate + new int3(0, 0, 1));
            }
        }

        [BurstCompile]
        public static void DestroyVoxel(in ChunkBufferingSystemData systemData, in BlockSystemData blockSystemData, in LightingSystemData lightingSystemData, in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 voxelCoordinate) {
            var chunkCoordinate = new int3 {
                x = (int)math.floor(voxelCoordinate.x / (float)Chunk.Size),
                y = (int)math.floor(voxelCoordinate.y / (float)Chunk.Size),
                z = (int)math.floor(voxelCoordinate.z / (float)Chunk.Size)
            };

            GetEntity(systemData, chunkCoordinate, out var entity);
            if (entity == Entity.Null
                || !entityManager.HasComponent<Chunk>(entity)
                || entityManager.HasComponent<RawChunk>(entity)
                || !entityManager.HasComponent<Sunlight>(entity)
                || entityManager.HasComponent<IncompleteLighting>(entity)
                || entityManager.HasComponent<RawChunk>(entity)) {
                return;
            }

            var localVoxelCoordinate = voxelCoordinate - chunkCoordinate * Chunk.Size;

            var chunk = entityManager.GetComponentData<Chunk>(entity);
            var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
            var voxel = chunk.Voxels[index];
            voxel.Type = BlockType.Air;
            chunk.Voxels[index] = voxel;
            entityManager.AddComponent<DirtyChunk>(entity);
            entityManager.AddComponent<ImmediateChunk>(entity);
            MarkDirtyIfNeededImmediate(systemData, entityManager, commandBuffer, chunkCoordinate, localVoxelCoordinate);

            LightingSystem.RemoveLight(lightingSystemData, systemData, entityManager, commandBuffer, voxelCoordinate, LightChanel.Red);
            LightingSystem.RemoveLight(lightingSystemData, systemData, entityManager, commandBuffer, voxelCoordinate, LightChanel.Green);
            LightingSystem.RemoveLight(lightingSystemData, systemData, entityManager, commandBuffer, voxelCoordinate, LightChanel.Blue);
            LightingSystem.Calculate(lightingSystemData, blockSystemData, systemData, entityManager, commandBuffer, LightChanel.Red);
            LightingSystem.Calculate(lightingSystemData, blockSystemData, systemData, entityManager, commandBuffer, LightChanel.Green);
            LightingSystem.Calculate(lightingSystemData, blockSystemData, systemData, entityManager, commandBuffer, LightChanel.Blue);

            GetVoxel(systemData, entityManager, voxelCoordinate + new int3(0, 1, 0), out var topVoxel);
            if (topVoxel.Type == BlockType.Air && topVoxel.Light.Sun == Light.Max) {
                for (int y = voxelCoordinate.y; y >= 0; y--) {
                    var bottomVoxelCoordinate = new int3(voxelCoordinate.x, y, voxelCoordinate.z);
                    GetVoxel(systemData, entityManager, bottomVoxelCoordinate, out var bottomVoxel);
                    if (bottomVoxel.Type != BlockType.Air) {
                        break;
                    }

                    LightingSystem.AddLight(lightingSystemData, systemData, entityManager, commandBuffer, bottomVoxelCoordinate, LightChanel.Sun, Light.Max);
                }
            }

            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(1, 0, 0), LightChanel.Red);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(-1, 0, 0), LightChanel.Red);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, 1, 0), LightChanel.Red);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, -1, 0), LightChanel.Red);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, 0, 1), LightChanel.Red);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, 0, -1), LightChanel.Red);
            LightingSystem.Calculate(lightingSystemData, blockSystemData, systemData, entityManager, commandBuffer, LightChanel.Red);

            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(1, 0, 0), LightChanel.Green);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(-1, 0, 0), LightChanel.Green);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, 1, 0), LightChanel.Green);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, -1, 0), LightChanel.Green);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, 0, 1), LightChanel.Green);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, 0, -1), LightChanel.Green);
            LightingSystem.Calculate(lightingSystemData, blockSystemData, systemData, entityManager, commandBuffer, LightChanel.Green);

            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(1, 0, 0), LightChanel.Blue);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(-1, 0, 0), LightChanel.Blue);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, 1, 0), LightChanel.Blue);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, -1, 0), LightChanel.Blue);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, 0, 1), LightChanel.Blue);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, 0, -1), LightChanel.Blue);
            LightingSystem.Calculate(lightingSystemData, blockSystemData, systemData, entityManager, commandBuffer, LightChanel.Blue);

            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(1, 0, 0), LightChanel.Sun);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(-1, 0, 0), LightChanel.Sun);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, 1, 0), LightChanel.Sun);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, -1, 0), LightChanel.Sun);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, 0, 1), LightChanel.Sun);
            LightingSystem.AddLight(lightingSystemData, systemData, entityManager, voxelCoordinate + new int3(0, 0, -1), LightChanel.Sun);
            LightingSystem.Calculate(lightingSystemData, blockSystemData, systemData, entityManager, commandBuffer, LightChanel.Sun);
        }

        [BurstCompile]
        public static void PlaceVoxel(in ChunkBufferingSystemData systemData, in BlockSystemData blockSystemData, in LightingSystemData lightingSystemData, in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 voxelCoordinate, BlockType blockType) {
            var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);

            GetEntity(systemData, chunkCoordinate, out var entity);
            if (entity == Entity.Null
                || !entityManager.HasComponent<Chunk>(entity)
                || entityManager.HasComponent<RawChunk>(entity)
                //|| !entityManager.HasComponent<Sunlight>(entity)
                //|| entityManager.HasComponent<IncompleteLighting>(entity)
                || entityManager.HasComponent<RawChunk>(entity)) {
                return;
            }

            var localVoxelCoordinate = voxelCoordinate - chunkCoordinate * Chunk.Size;

            var chunk = entityManager.GetComponentData<Chunk>(entity);
            var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
            var voxel = chunk.Voxels[index];
            voxel.Type = blockType;
            chunk.Voxels[index] = voxel;
            commandBuffer.AddComponent<DirtyChunk>(entity);
            commandBuffer.AddComponent<ImmediateChunk>(entity);
            MarkDirtyIfNeededImmediate(systemData, entityManager, commandBuffer, chunkCoordinate, localVoxelCoordinate);

            LightingSystem.RemoveLight(lightingSystemData, systemData, entityManager, commandBuffer, voxelCoordinate, LightChanel.Red);
            LightingSystem.RemoveLight(lightingSystemData, systemData, entityManager, commandBuffer, voxelCoordinate, LightChanel.Green);
            LightingSystem.RemoveLight(lightingSystemData, systemData, entityManager, commandBuffer, voxelCoordinate, LightChanel.Blue);
            LightingSystem.RemoveLight(lightingSystemData, systemData, entityManager, commandBuffer, voxelCoordinate, LightChanel.Sun);

            for (int y = voxelCoordinate.y - 1; y >= 0; y--) {
                var bottomVoxelCoordinate = new int3(voxelCoordinate.x, y, voxelCoordinate.z);
                GetVoxel(systemData, entityManager, bottomVoxelCoordinate, out var bottomVoxel);
                if (!blockSystemData.Blocks[(int)bottomVoxel.Type].IsTransparent) {
                    break;
                }

                LightingSystem.RemoveLight(lightingSystemData, systemData, entityManager, commandBuffer, bottomVoxelCoordinate, LightChanel.Sun);
            }

            LightingSystem.Calculate(lightingSystemData, blockSystemData, systemData, entityManager, commandBuffer, LightChanel.Red);
            LightingSystem.Calculate(lightingSystemData, blockSystemData, systemData, entityManager, commandBuffer, LightChanel.Green);
            LightingSystem.Calculate(lightingSystemData, blockSystemData, systemData, entityManager, commandBuffer, LightChanel.Blue);
            LightingSystem.Calculate(lightingSystemData, blockSystemData, systemData, entityManager, commandBuffer, LightChanel.Sun);

            var emission = blockSystemData.Blocks[(int)blockType].Emission;

            if (emission.Red != 0) {
                LightingSystem.AddLight(lightingSystemData, systemData, entityManager, commandBuffer, voxelCoordinate, LightChanel.Red, emission.Red);
                LightingSystem.Calculate(lightingSystemData, blockSystemData, systemData, entityManager, commandBuffer, LightChanel.Red);
            }

            if (emission.Green != 0) {
                LightingSystem.AddLight(lightingSystemData, systemData, entityManager, commandBuffer, voxelCoordinate, LightChanel.Green, emission.Green);
                LightingSystem.Calculate(lightingSystemData, blockSystemData, systemData, entityManager, commandBuffer, LightChanel.Green);
            }

            if (emission.Blue != 0) {
                LightingSystem.AddLight(lightingSystemData, systemData, entityManager, commandBuffer, voxelCoordinate, LightChanel.Blue, emission.Blue);
                LightingSystem.Calculate(lightingSystemData, blockSystemData, systemData, entityManager, commandBuffer, LightChanel.Blue);
            }
        }

        [BurstCompile]
        private static bool IsOutOfBuffer(int x, int z, int size) {
            return x < 0
                || z < 0
                || x >= size
                || z >= size;
        }

        [BurstCompile]
        private static void UpdateMetrics(ref ChunkBufferingSystemData systemData, int newDrawDistance) {
            var oldChunksSize = systemData.ChunksSize;
            var oldChunks = systemData.Chunks;
            systemData.DrawDistance = newDrawDistance;
            systemData.ChunksSize = systemData.DrawDistance * 2 + 1 + BufferDistance * 2;
            var chunksVolume = systemData.ChunksSize * systemData.ChunksSize * systemData.Height;

            systemData.Chunks = new NativeArray<Entity>(chunksVolume, Allocator.Persistent);
            if (systemData.ChunksBuffer.IsCreated) {
                systemData.ChunksBuffer.Dispose();
            }

            systemData.ChunksBuffer = new NativeArray<Entity>(chunksVolume, Allocator.Persistent);

            var sideDelta = oldChunksSize - systemData.ChunksSize;
            for (int x = 0; x < oldChunksSize; x++) {
                for (int z = 0; z < oldChunksSize; z++) {
                    for (int y = 0; y < systemData.Height; y++) {
                        var index = IndexUtility.ToIndex(x, y, z, oldChunksSize, systemData.Height);

                        var chunk = oldChunks[index];
                        if (chunk == Entity.Null) {
                            continue;
                        }

                        int newX = x - sideDelta / 2;
                        int newZ = z - sideDelta / 2;
                        if (IsOutOfBuffer(newX, newZ, systemData.ChunksSize)) {
                            continue;
                        }

                        systemData.Chunks[IndexUtility.ToIndex(newX, y, newZ, systemData.ChunksSize, systemData.Height)] = chunk;
                    }
                }
            }

            if (oldChunks.IsCreated) {
                oldChunks.Dispose();
            }
        }

        [BurstCompile]
        private static void UpdateBuffer(ref ChunkBufferingSystemData systemData, in int2 newCenter, in EntityCommandBuffer commandBuffer) {
            for (int i = 0; i < systemData.ChunksBuffer.Length; i++) {
                systemData.ChunksBuffer[i] = Entity.Null;
            }

            var centerDelta = newCenter - systemData.Center;
            for (int x = 0; x < systemData.ChunksSize; x++) {
                for (int z = 0; z < systemData.ChunksSize; z++) {
                    for (int y = 0; y < systemData.Height; y++) {
                        var index = IndexUtility.ToIndex(x, y, z, systemData.ChunksSize, systemData.Height);

                        var chunk = systemData.Chunks[index];
                        if (chunk == Entity.Null) {
                            continue;
                        }

                        int newX = x - centerDelta.x;
                        int newZ = z - centerDelta.y;
                        if (IsOutOfBuffer(newX, newZ, systemData.ChunksSize)) {
                            commandBuffer.DestroyEntity(chunk);
                            continue;
                        }

                        var newIndex = IndexUtility.ToIndex(newX, y, newZ, systemData.ChunksSize, systemData.Height);
                        systemData.ChunksBuffer[newIndex] = chunk;
                    }
                }
            }

            (systemData.ChunksBuffer, systemData.Chunks) = (systemData.Chunks, systemData.ChunksBuffer);

            systemData.Center = newCenter;
        }

        [BurstCompile]
        private void GenerateLoadData(ref ChunkLoadData loadData, in ChunkBufferingSystemData systemData, in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int2 column, int height, int distance) {
            if (loadData.Data.IsCreated) {
                loadData.Data.Dispose();
            }

            loadData.Data = new NativeList<ChunkLoadDescription>(Allocator.Persistent);

            int startX = column.x - distance - BufferDistance;
            int endX = column.x + distance + BufferDistance;
            int startZ = column.y - distance - BufferDistance;
            int endZ = column.y + distance + BufferDistance;

            int x = startX;
            int z = endZ;

            int startXBound = startX;
            int endXBound = endX;
            int startZBound = startZ;
            int endZBound = endZ;

            int size = distance * 2 + 1 + BufferDistance * 2;
            int length = size * size;
            int direction = 0;

            for (int i = 0; i < length; i++) {
                var lightRecalculationRequired = false;
                for (int y = 0; y < height; y++) {
                    var chunkCoordinate = new int3(x, y, z);

                    bool isRendered = x != startX && x != endX && z != startZ && z != endZ;
                    loadData.Data.Add(new ChunkLoadDescription {
                        Coordinate = chunkCoordinate,
                        IsRendered = isRendered
                    });

                    if (!lightRecalculationRequired) {
                        GetEntity(systemData, chunkCoordinate, out var chunkEntity);
                        if (chunkEntity == Entity.Null) {
                            lightRecalculationRequired = true;
                        }
                    }
                }

                if (lightRecalculationRequired) {
                    var requestEntity = entityManager.CreateEntity();
                    commandBuffer.AddComponent(requestEntity, new SunlightRequest {
                        Column = new int2(x, z)
                    });
                }

                switch (direction) {
                case 0:
                    ++x;
                    break;
                case 1:
                    --z;
                    break;
                case 2:
                    --x;
                    break;
                case 3:
                    ++z;
                    break;
                }

                if (direction == 0 && x == endXBound) {
                    direction = 1;
                } else if (direction == 1 && z == startZBound) {
                    direction = 2;
                    ++startZBound;
                } else if (direction == 2 && x == startXBound) {
                    direction = 3;
                    --endZBound;
                    ++startXBound;
                } else if (direction == 3 && z == endZBound) {
                    direction = 0;
                    --endXBound;
                }
            }

            loadData.Sequence = loadData.Data.Length - 1;
        }

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state) {
            state.EntityManager.AddComponent<ChunkLoadData>(state.SystemHandle);
            state.EntityManager.AddComponentData(state.SystemHandle, new ChunkBufferingSystemData {
                Height = 16
            });

            var requestEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(requestEntity, new ChunkBufferingRequest {
                NewDrawDistance = 4,
            });
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            var systemData = state.EntityManager.GetComponentDataRW<ChunkBufferingSystemData>(state.SystemHandle);

            foreach (var (request, entity) in SystemAPI
                .Query<RefRO<ChunkBufferingRequest>>()
                .WithEntityAccess()) {
                UpdateMetrics(ref systemData.ValueRW, request.ValueRO.NewDrawDistance);
                commandBuffer.DestroyEntity(entity);

                var reloadingRequestEntity = state.EntityManager.CreateEntity();
                commandBuffer.AddComponent<ChunkReloadingRequest>(reloadingRequestEntity);
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();

            commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (_, entity) in SystemAPI
                .Query<ChunkReloadingRequest>()
                .WithEntityAccess()) {
                var loadingRequestEntity = state.EntityManager.CreateEntity();
                var loadData = state.EntityManager.GetComponentDataRW<ChunkLoadData>(state.SystemHandle);
                commandBuffer.AddComponent(loadingRequestEntity, new ChunkLoadingRequest {
                    NewCenter = loadData.ValueRO.LastPlayerColumn
                });

                commandBuffer.DestroyEntity(entity);
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();

            commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var localToWorld in SystemAPI
                .Query<RefRO<LocalToWorld>>()
                .WithAll<PlayerMovement>()) {

                var position = localToWorld.ValueRO.Position;
                var column = new int2 {
                    x = (int)math.floor(position.x / Chunk.Size),
                    y = (int)math.floor(position.z / Chunk.Size)
                };

                var loadData = state.EntityManager.GetComponentDataRW<ChunkLoadData>(state.SystemHandle);
                var lastPlayerColumn = loadData.ValueRO.LastPlayerColumn;
                if (lastPlayerColumn.x != column.x || lastPlayerColumn.y != column.y) {
                    loadData.ValueRW.LastPlayerColumn = column;
                    var requestEntity = state.EntityManager.CreateEntity();
                    commandBuffer.AddComponent(requestEntity, new ChunkLoadingRequest {
                        NewCenter = column
                    });
                }
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();

            commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            systemData = state.EntityManager.GetComponentDataRW<ChunkBufferingSystemData>(state.SystemHandle);
            var chunkLoadData = state.EntityManager.GetComponentDataRW<ChunkLoadData>(state.SystemHandle);

            foreach (var (request, entity) in SystemAPI.Query<ChunkLoadingRequest>().WithEntityAccess()) {
                UpdateBuffer(ref systemData.ValueRW, request.NewCenter, commandBuffer);
                GenerateLoadData(ref chunkLoadData.ValueRW, systemData.ValueRO, state.EntityManager, commandBuffer, request.NewCenter, systemData.ValueRO.Height, systemData.ValueRO.DrawDistance);
                commandBuffer.DestroyEntity(entity);
            }

            systemData = state.EntityManager.GetComponentDataRW<ChunkBufferingSystemData>(state.SystemHandle);
            chunkLoadData = state.EntityManager.GetComponentDataRW<ChunkLoadData>(state.SystemHandle);

            if (chunkLoadData.ValueRO.Data.IsCreated) {
                for (int i = chunkLoadData.ValueRO.Sequence; i >= 0; i--) {
                    var item = chunkLoadData.ValueRO.Data[i];
                    GetEntity(systemData.ValueRO, item.Coordinate, out var chunkEntity);
                    if (chunkEntity != Entity.Null) {
                        if (item.IsRendered) {
                            if (state.EntityManager.HasComponent<DisableRendering>(chunkEntity)) {
                                commandBuffer.RemoveComponent<DisableRendering>(chunkEntity);
                            }
                        } else {
                            if (!state.EntityManager.HasComponent<DisableRendering>(chunkEntity)) {
                                commandBuffer.AddComponent<DisableRendering>(chunkEntity);
                            }
                        }
                    } else {
                        var newChunkEntity = state.EntityManager.CreateEntity();
                        commandBuffer.AddComponent(newChunkEntity, new ChunkSpawnRequest {
                            Coordinate = item.Coordinate,
                            HasRenderer = item.IsRendered
                        });
                        var index = ToIndex(systemData.ValueRO, item.Coordinate);
                        systemData.ValueRW.Chunks[index] = newChunkEntity;
                        chunkLoadData.ValueRW.Sequence = i;
                        break;
                    }
                }
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
}