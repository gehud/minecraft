using Minecraft.Components;
using Minecraft.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Minecraft.Systems {
    [UpdateBefore(typeof(ChunkSystem))]
    public partial class ChunkBufferingSystem : SystemBase {
        public static int ChunkToIndex(in ChunkBuffer chunkBuffer, int3 coordinate) {
            var arrayCoordinate = new int3 {
                x = coordinate.x - chunkBuffer.Center.x + chunkBuffer.DrawDistance + 1,
                y = coordinate.y,
                z = coordinate.z - chunkBuffer.Center.y + chunkBuffer.DrawDistance + 1
            };

            return Array3DUtility.To1D(arrayCoordinate.x, arrayCoordinate.y, arrayCoordinate.z, chunkBuffer.ChunksSize, ChunkBuffer.HEIGHT);
        }

        public static bool HasChunk(in ChunkBuffer chunkBuffer, int3 coordinate) {
#if DEBUG // With bounds check.
            var arrayCoordinate = new int3 {
                x = coordinate.x - chunkBuffer.Center.x + chunkBuffer.DrawDistance + 1,
                y = coordinate.y,
                z = coordinate.z - chunkBuffer.Center.y + chunkBuffer.DrawDistance + 1
            };

            if (arrayCoordinate.x < 0 ||
                arrayCoordinate.y < 0 ||
                arrayCoordinate.z < 0 ||
                arrayCoordinate.x >= chunkBuffer.ChunksSize ||
                arrayCoordinate.y >= ChunkBuffer.HEIGHT ||
                arrayCoordinate.z >= chunkBuffer.ChunksSize) {
                return false;
            }

            var index = Array3DUtility.To1D(arrayCoordinate.x, arrayCoordinate.y, arrayCoordinate.z, chunkBuffer.ChunksSize, ChunkBuffer.HEIGHT);
            return chunkBuffer.Chunks[index] != Entity.Null;
#else
			var index = ChunkToIndex(chunkBuffer, coordinate);
			if (index < 0 || index >= chunkBuffer.Chunks.Length) {
				return false;
			}

			return chunkBuffer.Chunks[index] != Entity.Null;
#endif
        }

        public static Entity GetChunk(in ChunkBuffer chunkBuffer, int3 coordinate) {
#if DEBUG // With bounds check.
            var arrayCoordinate = new int3 {
                x = coordinate.x - chunkBuffer.Center.x + chunkBuffer.DrawDistance + 1,
                y = coordinate.y,
                z = coordinate.z - chunkBuffer.Center.y + chunkBuffer.DrawDistance + 1
            };

            if (arrayCoordinate.x < 0 ||
                arrayCoordinate.y < 0 ||
                arrayCoordinate.z < 0 ||
                arrayCoordinate.x >= chunkBuffer.ChunksSize ||
                arrayCoordinate.y >= ChunkBuffer.HEIGHT ||
                arrayCoordinate.z >= chunkBuffer.ChunksSize) {
                return Entity.Null;
            }

            var index = Array3DUtility.To1D(arrayCoordinate.x, arrayCoordinate.y, arrayCoordinate.z, chunkBuffer.ChunksSize, ChunkBuffer.HEIGHT);
            return chunkBuffer.Chunks[index];
#else
			var index = ChunkToIndex(chunkBuffer, coordinate);
			if (index < 0 || index >= chunkBuffer.Chunks.Length) {
				return Entity.Null;
			}

			return chunkBuffer.Chunks[index];
#endif
        }

        public static Voxel GetVoxel(EntityManager entityManager, in ChunkBuffer chunkBuffer, int3 coordinate) {
            var chunkCoordinate = new int3 {
                x = (int)math.floor(coordinate.x / (float)Chunk.SIZE),
                y = (int)math.floor(coordinate.y / (float)Chunk.SIZE),
                z = (int)math.floor(coordinate.z / (float)Chunk.SIZE)
            };

            var chunk = GetChunk(chunkBuffer, chunkCoordinate);
            if (chunk == Entity.Null) {
                return default;
            }

            var localVoxelCoordinate = coordinate - chunkCoordinate * Chunk.SIZE;
            var localVoxelIndex = Array3DUtility.To1D(localVoxelCoordinate, Chunk.SIZE, Chunk.SIZE);

            return entityManager.GetComponentData<Chunk>(chunk).Voxels[localVoxelIndex];
        }

        private static void UpdateMetrics(ref ChunkBuffer chunkBuffer, int newDrawDistance) {
            var oldChunksSize = chunkBuffer.ChunksSize;
            var oldChunks = chunkBuffer.Chunks;
            chunkBuffer.DrawDistance = newDrawDistance;
            chunkBuffer.ChunksSize = chunkBuffer.DrawDistance * 2 + 3;
            var chunksVolume = chunkBuffer.ChunksSize * chunkBuffer.ChunksSize * ChunkBuffer.HEIGHT;

            chunkBuffer.Chunks = new NativeArray<Entity>(chunksVolume, Allocator.Persistent);
            if (chunkBuffer.ChunksBuffer.IsCreated) {
                chunkBuffer.ChunksBuffer.Dispose();
            }

            chunkBuffer.ChunksBuffer = new NativeArray<Entity>(chunksVolume, Allocator.Persistent);

            var delta = oldChunksSize - chunkBuffer.ChunksSize;
            for (int x = 0; x < oldChunksSize; x++) {
                for (int z = 0; z < oldChunksSize; z++) {
                    for (int y = 0; y < ChunkBuffer.HEIGHT; y++) {
                        var index = Array3DUtility.To1D(x, y, z, oldChunksSize, ChunkBuffer.HEIGHT);

                        var chunk = oldChunks[index];
                        if (chunk == Entity.Null) {
                            continue;
                        }

                        int nx = x - delta / 2;
                        int nz = z - delta / 2;
                        if (nx < 0 || nz < 0 || nx >= chunkBuffer.ChunksSize || nz >= chunkBuffer.ChunksSize) {
                            continue;
                        }

                        chunkBuffer.Chunks[Array3DUtility.To1D(nx, y, nz, chunkBuffer.ChunksSize, ChunkBuffer.HEIGHT)] = chunk;
                    }
                }
            }

            if (oldChunks.IsCreated) {
                oldChunks.Dispose();
            }
        }

        private static void UpdateBuffer(EntityManager entityManager, ref ChunkBuffer chunkBuffer, int2 newCenter) {
            for (int i = 0; i < chunkBuffer.ChunksBuffer.Length; i++) {
                chunkBuffer.ChunksBuffer[i] = Entity.Null;
            }

            var centerDelta = newCenter - chunkBuffer.Center;
            for (int x = 0; x < chunkBuffer.ChunksSize; x++) {
                for (int z = 0; z < chunkBuffer.ChunksSize; z++) {
                    for (int y = 0; y < ChunkBuffer.HEIGHT; y++) {
                        var index = Array3DUtility.To1D(x, y, z, chunkBuffer.ChunksSize, ChunkBuffer.HEIGHT);

                        var chunk = chunkBuffer.Chunks[index];
                        if (chunk == Entity.Null) {
                            continue;
                        }

                        int nx = x - centerDelta.x;
                        int nz = z - centerDelta.y;
                        if (nx < 0 || nz < 0 || nx >= chunkBuffer.ChunksSize || nz >= chunkBuffer.ChunksSize) {
                            entityManager.DestroyEntity(chunk);
                            continue;
                        }

                        var newIndex = Array3DUtility.To1D(nx, y, nz, chunkBuffer.ChunksSize, ChunkBuffer.HEIGHT);
                        chunkBuffer.ChunksBuffer[newIndex] = chunk;
                    }
                }
            }

            (chunkBuffer.ChunksBuffer, chunkBuffer.Chunks) = (chunkBuffer.Chunks, chunkBuffer.ChunksBuffer);

            chunkBuffer.Center = newCenter;
        }

        private void GenerateLoadData(ref ChunkLoadData loadData, int2 column, int distance) {
            loadData.Data = new NativeList<ChunkLoadDescription>(Allocator.TempJob);

            int startX = column.x - distance - 1;
            int endX = column.x + distance + 1;
            int startZ = column.y - distance - 1;
            int endZ = column.y + distance + 1;

            int x = startX;
            int z = endZ;

            int startXBound = startX;
            int endXBound = endX;
            int startZBound = startZ;
            int endZBound = endZ;

            int size = distance * 2 + 3;
            int length = size * size;
            int direction = 0;

            for (int i = 0; i < length; i++) {
                for (int y = 0; y < ChunkBuffer.HEIGHT; y++) {
                    var chunkCoordinate = new int3(x, y, z);

                    bool isRendered = x != startX && x != endX && z != startZ && z != endZ;
                    loadData.Data.Add(new ChunkLoadDescription {
                        Coordinate = chunkCoordinate,
                        IsRendered = isRendered
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

        }

        protected override void OnCreate() {
            EntityManager.AddComponent<ChunkBuffer>(SystemHandle);
            EntityManager.AddComponent<ChunkLoadData>(SystemHandle);

            var requestEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(requestEntity, new ChunkBufferingRequest {
                NewDrawDistance = 2,
            });
        }

        protected override void OnUpdate() {
            Entities.ForEach((Entity entity, in ChunkBufferingRequest request) => {
                var buffer = EntityManager.GetComponentDataRW<ChunkBuffer>(SystemHandle);
                UpdateMetrics(ref buffer.ValueRW, request.NewDrawDistance);
                EntityManager.DestroyEntity(entity);

                var reloadingRequestEntity = EntityManager.CreateEntity();
                EntityManager.AddComponent<ChunkReloadingRequest>(reloadingRequestEntity);
            }).WithStructuralChanges().Run();

            Entities.ForEach((Entity entity, in ChunkReloadingRequest request) => {
                var loadingRequestEntity = EntityManager.CreateEntity();
                var loadData = EntityManager.GetComponentDataRW<ChunkLoadData>(SystemHandle);
                EntityManager.AddComponentData(loadingRequestEntity, new ChunkLoadingRequest {
                    NewCenter = loadData.ValueRO.LastPlayerColumn
                });
                EntityManager.DestroyEntity(entity);
            }).WithStructuralChanges().Run();

            Entities.WithAll<PlayerMovement>().ForEach((in LocalToWorld localToWorld) => {
                var position = localToWorld.Position;
                var column = new int2 {
                    x = (int)math.floor(position.x / Chunk.SIZE),
                    y = (int)math.floor(position.z / Chunk.SIZE)
                };

                var loadData = EntityManager.GetComponentDataRW<ChunkLoadData>(SystemHandle);
                var lastPlayerColumn = loadData.ValueRO.LastPlayerColumn;
                if (lastPlayerColumn.x != column.x || lastPlayerColumn.y != column.y) {
                    loadData.ValueRW.LastPlayerColumn = column;
                    var requestEntity = EntityManager.CreateEntity();
                    EntityManager.AddComponentData(requestEntity, new ChunkLoadingRequest {
                        NewCenter = column
                    });
                }
            }).WithStructuralChanges().Run();

            Entities.ForEach((Entity entity, in ChunkLoadingRequest request) => {
                UpdateBuffer(EntityManager, ref EntityManager.GetComponentDataRW<ChunkBuffer>(SystemHandle).ValueRW, request.NewCenter);

                GenerateLoadData(ref EntityManager.GetComponentDataRW<ChunkLoadData>(SystemHandle).ValueRW, request.NewCenter, EntityManager.GetComponentDataRW<ChunkBuffer>(SystemHandle).ValueRO.DrawDistance);

                foreach (var item in EntityManager.GetComponentDataRW<ChunkLoadData>(SystemHandle).ValueRO.Data) {
                    if (!HasChunk(EntityManager.GetComponentDataRW<ChunkBuffer>(SystemHandle).ValueRO, item.Coordinate)) {
                        var newChunk = EntityManager.CreateEntity();
                        EntityManager.AddComponentData(newChunk, new ChunkInitializer {
                            Coordinate = item.Coordinate,
                            HasRenderer = item.IsRendered
                        });
                        var index = ChunkToIndex(EntityManager.GetComponentDataRW<ChunkBuffer>(SystemHandle).ValueRO, item.Coordinate);
                        EntityManager.GetComponentDataRW<ChunkBuffer>(SystemHandle).ValueRW.Chunks[index] = newChunk;
                    } else {
                        var chunk = GetChunk(EntityManager.GetComponentDataRW<ChunkBuffer>(SystemHandle).ValueRO, item.Coordinate);

                        if (EntityManager.HasComponent<ChunkInitializer>(chunk)) {
                            var initializer = EntityManager.GetComponentData<ChunkInitializer>(chunk);
                            initializer.HasRenderer = item.IsRendered;
                            EntityManager.SetComponentData(chunk, initializer);
                        } else {
                            if (item.IsRendered) {
                                if (EntityManager.HasComponent<DisableRendering>(chunk)) {
                                    EntityManager.RemoveComponent<DisableRendering>(chunk);
                                }
                            } else {
                                if (!EntityManager.HasComponent<DisableRendering>(chunk)) {
                                    EntityManager.AddComponent<DisableRendering>(chunk);
                                }
                            }
                        }
                    }
                }

                EntityManager.GetComponentDataRW<ChunkLoadData>(SystemHandle).ValueRW.Data.Dispose();

                EntityManager.DestroyEntity(entity);
            }).WithStructuralChanges().Run();
        }
    }
}