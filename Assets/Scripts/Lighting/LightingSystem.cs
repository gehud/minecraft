using Minecraft.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Minecraft.Lighting {
    [BurstCompile]
    [UpdateAfter(typeof(ChunkGenerationSystem))]
    public partial struct LightingSystem : ISystem {
        public const int ChanelCount = 4;

        private static readonly int3[] blockSides = {
            new( 0,  0,  1),
            new( 0,  0, -1),
            new( 0,  1,  0),
            new( 0, -1,  0),
            new( 1,  0,  0),
            new(-1,  0,  0),
        };

        private struct ScheduledJob {
            public SunlightCalculationJob Data;
            public JobHandle Handle;
        }

        private NativeList<ScheduledJob> jobs;

        [BurstCompile]
        public static void AddLight(in LightingSystemData systemData, in ChunkBufferingSystemData chunkBufferingSystemData, in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 voxelCoordinate, LightChanel chanel, byte level) {
            if (level <= 1) {
                return;
            }

            var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);

            ChunkBufferingSystem.GetEntity(chunkBufferingSystemData, chunkCoordinate, out var entity);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity)) {
                return;
            }

            var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
            var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
            var voxels = entityManager.GetComponentData<Chunk>(entity).Voxels;
            var voxel = voxels[index];
            voxel.Light.Set(chanel, level);
            voxels[index] = voxel;

            commandBuffer.SetComponentEnabled<DirtyChunk>(entity, true);
            commandBuffer.SetComponentEnabled<ImmediateChunk>(entity, true);

            ChunkBufferingSystem.MarkDirtyIfNeededImmediate(chunkBufferingSystemData, entityManager, commandBuffer, chunkCoordinate, localVoxelCoordinate);

            var entry = new LightingEntry(voxelCoordinate, level);
            systemData.AddQueues[(int)chanel].Enqueue(entry);
        }

        [BurstCompile]
        public static void AddLight(in LightingSystemData systemData, in ChunkBufferingSystemData chunkBufferingSystemData, in EntityManager entityManager, in int3 voxelCoordinate, LightChanel chanel) {
            var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
            ChunkBufferingSystem.GetEntity(chunkBufferingSystemData, chunkCoordinate, out var entity);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity)) {
                return;
            }

            var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
            var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
            var voxels = entityManager.GetComponentData<Chunk>(entity).Voxels;
            var level = voxels[index].Light.Get(chanel);
            if (level <= 1) {
                return;
            }

            var entry = new LightingEntry(voxelCoordinate, level);
            systemData.AddQueues[(int)chanel].Enqueue(entry);
        }

        [BurstCompile]
        public static void RemoveLight(in LightingSystemData systemData, in ChunkBufferingSystemData chunkBufferingSystemData, in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 voxelCoordinate, LightChanel chanel) {
            var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
            ChunkBufferingSystem.GetEntity(chunkBufferingSystemData, chunkCoordinate, out var entity);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity)) {
                return;
            }

            var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
            var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
            var voxels = entityManager.GetComponentData<Chunk>(entity).Voxels;
            var voxel = voxels[index];
            byte level = voxel.Light.Get(chanel);
            if (level <= 1) {
                return;
            }

            voxel.Light.Set(chanel, Light.Min);
            voxels[index] = voxel;

            commandBuffer.SetComponentEnabled<DirtyChunk>(entity, true);
            commandBuffer.SetComponentEnabled<ImmediateChunk>(entity, true);

            ChunkBufferingSystem.MarkDirtyIfNeededImmediate(chunkBufferingSystemData, entityManager, commandBuffer, chunkCoordinate, localVoxelCoordinate);

            var entry = new LightingEntry(voxelCoordinate, level);
            systemData.RemoveQueues[(int)chanel].Enqueue(entry);
        }

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state) {
            var addQueues = new NativeArray<NativeQueue<LightingEntry>>(ChanelCount, Allocator.Persistent);
            for (int i = 0; i < addQueues.Length; i++) {
                addQueues[i] = new NativeQueue<LightingEntry>(Allocator.Persistent);
            }

            var removeQueues = new NativeArray<NativeQueue<LightingEntry>>(ChanelCount, Allocator.Persistent);
            for (int i = 0; i < removeQueues.Length; i++) {
                removeQueues[i] = new NativeQueue<LightingEntry>(Allocator.Persistent);
            }

            state.EntityManager.AddComponentData(state.SystemHandle, new LightingSystemData {
                AddQueues = addQueues,
                RemoveQueues = removeQueues
            });

            jobs = new NativeList<ScheduledJob>(Allocator.Persistent);
        }

        [BurstCompile]
        public static void Calculate(in LightingSystemData systemData, in BlockSystemData blockSystemData, in ChunkBufferingSystemData chunkBufferingSystemData, in EntityManager entityManager, in EntityCommandBuffer commandBuffer, LightChanel chanel) {
            while (systemData.RemoveQueues[(int)chanel].TryDequeue(out var entry)) {
                for (int i = 0; i < blockSides.Length; i++) {
                    var voxelCoordinate = entry.Coordinate + blockSides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
                    ChunkBufferingSystem.GetEntity(chunkBufferingSystemData, chunkCoordinate, out var entity);
                    if (entity != Entity.Null && entityManager.HasComponent<Chunk>(entity)) {
                        var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
                        var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
                        var voxels = entityManager.GetComponentData<Chunk>(entity).Voxels;
                        var voxel = voxels[index];
                        var level = voxel.Light.Get(chanel);
                        var blockType = voxels[index].Type;
                        var absorption = blockSystemData.Blocks[(int)blockType].Absorption;
                        if (level != 0 && level == entry.Level - absorption - 1) {
                            var removeEntry = new LightingEntry(voxelCoordinate, level);
                            systemData.RemoveQueues[(int)chanel].Enqueue(removeEntry);
                            voxel.Light.Set(chanel, Light.Min);
                            voxels[index] = voxel;
                            commandBuffer.SetComponentEnabled<DirtyChunk>(entity, true);
                            commandBuffer.SetComponentEnabled<ImmediateChunk>(entity, true);
                            ChunkBufferingSystem.MarkDirtyIfNeededImmediate(chunkBufferingSystemData, entityManager, commandBuffer, chunkCoordinate, localVoxelCoordinate);
                        } else if (level >= entry.Level) {
                            var addEntry = new LightingEntry(voxelCoordinate, level);
                            systemData.AddQueues[(int)chanel].Enqueue(addEntry);
                        }
                    }
                }
            }

            while (systemData.AddQueues[(int)chanel].TryDequeue(out var entry)) {
                if (entry.Level <= 1) {
                    continue;
                }

                for (int i = 0; i < blockSides.Length; i++) {
                    var voxelCoordinate = entry.Coordinate + blockSides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
                    ChunkBufferingSystem.GetEntity(chunkBufferingSystemData, chunkCoordinate, out var entity);
                    if (entity != Entity.Null && entityManager.HasComponent<Chunk>(entity)) {
                        var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
                        var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
                        var voxels = entityManager.GetComponentData<Chunk>(entity).Voxels;
                        var voxel = voxels[index];
                        var level = voxel.Light.Get(chanel);
                        var blockType = voxels[index].Type;
                        var absorption = blockSystemData.Blocks[(int)blockType].Absorption;
                        if (blockSystemData.Blocks[(int)blockType].IsTransparent && level + absorption + 1 < entry.Level) {
                            var newLevel = (byte)(entry.Level - absorption - 1);
                            voxel.Light.Set(chanel, newLevel);
                            voxels[index] = voxel;
                            var addEntry = new LightingEntry(voxelCoordinate, newLevel);
                            systemData.AddQueues[(int)chanel].Enqueue(addEntry);
                            commandBuffer.SetComponentEnabled<DirtyChunk>(entity, true);
                            commandBuffer.SetComponentEnabled<ImmediateChunk>(entity, true);
                            ChunkBufferingSystem.MarkDirtyIfNeededImmediate(chunkBufferingSystemData, entityManager, commandBuffer, chunkCoordinate, localVoxelCoordinate);
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private bool TrySchedule(ref SystemState state, int columnClasterHeight, in SunlightRequest request, in RefRW<LightingSystemData> systemData, in RefRW<ChunkBufferingSystemData> chunkBufferingSystemData, in RefRW<BlockSystemData> blockSystemData) {
            var claster = new NativeArray<NativeArray<Voxel>>(3 * 3 * columnClasterHeight, Allocator.Persistent);

            var clasterEntities = new NativeArray<Entity>(3 * 3 * columnClasterHeight, Allocator.Temp);

            var origin = new int3 {
                x = request.Column.x - 1,
                y = -1,
                z = request.Column.y - 1
            };

            for (int i = 0; i < 3 * 3 * columnClasterHeight; i++) {
                var coordinate = origin + IndexUtility.ToCoordinate(i, 3, columnClasterHeight);
                ChunkBufferingSystem.GetEntity(chunkBufferingSystemData.ValueRO, coordinate, out var clasterEntity);
                bool isValidChunk = state.EntityManager.Exists(clasterEntity)
                    && state.EntityManager.HasComponent<Chunk>(clasterEntity)
                    && !state.EntityManager.IsComponentEnabled<DirtyChunk>(clasterEntity)
                    && !state.EntityManager.HasComponent<RawChunk>(clasterEntity)
                    && !state.EntityManager.IsComponentEnabled<ThreadedChunk>(clasterEntity);

                if (isValidChunk) {
                    claster[i] = state.EntityManager.GetComponentData<Chunk>(clasterEntity).Voxels;
                    clasterEntities[i] = clasterEntity;
                } else if (!isValidChunk
                    && coordinate.y != -1
                    && coordinate.y != chunkBufferingSystemData.ValueRO.Height) {
                    claster.Dispose();
                    clasterEntities.Dispose();
                    return false;
                }
            }

            foreach (var clasterEntity in clasterEntities) {
                if (clasterEntity != Entity.Null) {
                    state.EntityManager.SetComponentEnabled<ThreadedChunk>(clasterEntity, true);
                }
            }

            clasterEntities.Dispose();

            var job = new SunlightCalculationJob {
                Blocks = blockSystemData.ValueRO.Blocks,
                Column = request.Column,
                BufferHeight = chunkBufferingSystemData.ValueRO.Height,
                ClasterHeight = columnClasterHeight,
                Claster = claster,

                AddQueues = new NativeQueue<LightingEntry>(Allocator.Persistent),
                RemoveQueues = new NativeQueue<LightingEntry>(Allocator.Persistent),
            };

            var handle = job.Schedule();

            jobs.Add(new ScheduledJob {
                Data = job,
                Handle = handle
            });

            return true;
        }

        private bool TryCompleteJob(ref SystemState state, in ScheduledJob job, in RefRW<ChunkBufferingSystemData> chunkBufferingSystemData, in EntityCommandBuffer commandBuffer) {
            if (!job.Handle.IsCompleted) {
                return false;
            }

            job.Handle.Complete();

            for (int y = 0; y < chunkBufferingSystemData.ValueRO.Height; y++) {
                var chunkCoordinate = new int3(job.Data.Column.x, y, job.Data.Column.y);
                ChunkBufferingSystem.GetEntity(chunkBufferingSystemData.ValueRO, chunkCoordinate, out var chunkEntity);
                commandBuffer.AddComponent<Sunlight>(chunkEntity);
                commandBuffer.AddComponent<IncompleteLighting>(chunkEntity);
            }

            var origin = new int3 {
                x = job.Data.Column.x - 1,
                y = 0,
                z = job.Data.Column.y - 1
            };

            for (int i = 0; i < 3 * 3 * chunkBufferingSystemData.ValueRO.Height; i++) {
                var coordinate = origin + IndexUtility.ToCoordinate(i, 3, chunkBufferingSystemData.ValueRO.Height);
                ChunkBufferingSystem.GetEntity(chunkBufferingSystemData.ValueRO, coordinate, out var clasterEntity);
                state.EntityManager.SetComponentEnabled<ThreadedChunk>(clasterEntity, false);
            }

            job.Data.Dispose();

            return true;
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            var systemData = state.EntityManager.GetComponentDataRW<LightingSystemData>(state.SystemHandle);
            var blockSystemData = SystemAPI.GetSingletonRW<BlockSystemData>();
            var chunkBufferingSystemData = SystemAPI.GetSingletonRW<ChunkBufferingSystemData>();
            
            var columnClasterHeight = chunkBufferingSystemData.ValueRO.Height + 2;
            
            foreach (var (request, entity) in SystemAPI
                .Query<SunlightRequest>()
                .WithEntityAccess()) {

                if (!TrySchedule(ref state, columnClasterHeight, request, systemData, chunkBufferingSystemData, blockSystemData)) {
                    continue;
                }

                commandBuffer.DestroyEntity(entity);
            }

            for (int i = 0; i < jobs.Length; i++) {
                var job = jobs[i];

                if (TryCompleteJob(ref state, job, chunkBufferingSystemData, commandBuffer)) {
                    jobs.RemoveAt(i);
                }
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();

            commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            chunkBufferingSystemData = SystemAPI.GetSingletonRW<ChunkBufferingSystemData>();

            foreach (var (chunk, entity) in SystemAPI
                .Query<RefRO<Chunk>>()
                .WithAll<Sunlight>()
                .WithAll<IncompleteLighting>()
                .WithNone<DirtyChunk>()
                .WithEntityAccess()) {

                var chunkCoordinate = chunk.ValueRO.Coordinate;
                var origin = chunkCoordinate - new int3(1, 1, 1);

                var isValidClaster = true;
                for (int j = 0; j < 3 * 3 * 3; j++) {
                    var coordinate = origin + IndexUtility.ToCoordinate(j, 3, 3);
                    ChunkBufferingSystem.GetEntity(chunkBufferingSystemData.ValueRO, coordinate, out var sideChunk);
                    bool isValidChunk = state.EntityManager.Exists(sideChunk)
                        && state.EntityManager.HasComponent<Chunk>(sideChunk)
                        && !state.EntityManager.HasComponent<RawChunk>(sideChunk)
                        && state.EntityManager.HasComponent<Sunlight>(sideChunk);

                    if (coordinate.y != -1 && coordinate.y != chunkBufferingSystemData.ValueRO.Height && !isValidChunk) {
                        isValidClaster = false;
                        break;
                    }
                }

                if (isValidClaster) {
                    commandBuffer.SetComponentEnabled<DirtyChunk>(entity, true);
                    commandBuffer.RemoveComponent<IncompleteLighting>(entity);
                }
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state) {
            foreach (var job in jobs) {
                job.Handle.Complete();
                job.Data.Dispose();
            }
            
            jobs.Dispose();
        }
    }
}