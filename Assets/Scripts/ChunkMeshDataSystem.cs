using Minecraft.Lighting;
using Minecraft.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Minecraft {
    [BurstCompile]
    [UpdateAfter(typeof(LightingSystem))]
    public partial struct ChunkMeshDataSystem : ISystem {
        private struct ScheduledJob {
            public ChunkMeshDataJob Data;
            public JobHandle Handle;
        }

        private NativeList<ScheduledJob> jobs;

        [BurstCompile]
        private bool TryRun(ref SystemState state, in Chunk chunk, in Entity entity, in EntityCommandBuffer commandBuffer) {
            var claster = new NativeArray<NativeArray<Voxel>>(3 * 3 * 3, Allocator.TempJob);
            var origin = chunk.Coordinate - new int3(1, 1, 1);
            var chunkBufferingSystemData = SystemAPI.GetSingletonRW<ChunkBufferingSystemData>();

            for (int i = 0; i < 3 * 3 * 3; i++) {
                var coordinate = origin + IndexUtility.ToCoordinate(i, 3, 3);
                ChunkBufferingSystem.GetEntity(chunkBufferingSystemData.ValueRO, coordinate, out Entity clasterEntity);
                bool isValidChunk = state.EntityManager.Exists(clasterEntity)
                    && state.EntityManager.HasComponent<Chunk>(clasterEntity)
                    && !state.EntityManager.HasComponent<RawChunk>(clasterEntity)
                    && !state.EntityManager.IsComponentEnabled<ThreadedChunk>(clasterEntity);

                if (isValidChunk) {
                    claster[i] = state.EntityManager.GetComponentData<Chunk>(clasterEntity).Voxels;
                } else if (!isValidChunk 
                    && coordinate.y != -1
                    && coordinate.y != chunkBufferingSystemData.ValueRO.Height) {
                    claster.Dispose();
                    return false;
                }
            }

            var job = new ChunkMeshDataJob {
                Entity = entity,
                Coordinate = chunk.Coordinate,
                Vertices = new NativeList<Vertex>(Allocator.Persistent),
                OpaqueIndices = new NativeList<ushort>(Allocator.Persistent),
                TransparentIndices = new NativeList<ushort>(Allocator.Persistent),
                ChunkCoordinate = chunk.Coordinate,
                Claster = claster,
                Blocks = SystemAPI.GetSingletonRW<BlockSystemData>().ValueRO.Blocks
            };

            job.Schedule().Complete();

            job.Dispose();

            commandBuffer.AddComponent(entity, new ChunkMeshData {
                Vertices = job.Vertices.AsArray(),
                OpaqueIndices = job.OpaqueIndices.AsArray(),
                TransparentIndices = job.TransparentIndices.AsArray()
            });

            commandBuffer.SetComponentEnabled<DirtyChunk>(entity, false);

            return true;
        }

        [BurstCompile]
        private bool TrySchedule(ref SystemState state, in RefRW<ChunkBufferingSystemData> chunkBufferingSystemData, in Chunk chunk, in Entity entity) {
            var claster = new NativeArray<NativeArray<Voxel>>(3 * 3 * 3, Allocator.Persistent);
            var clasterEntities = new NativeArray<Entity>(3 * 3 * 3, Allocator.Temp);
            
            var origin = chunk.Coordinate - new int3(1, 1, 1);

            for (int i = 0; i < 3 * 3 * 3; i++) {
                var coordinate = origin + IndexUtility.ToCoordinate(i, 3, 3);
                ChunkBufferingSystem.GetEntity(chunkBufferingSystemData.ValueRO, coordinate, out Entity clasterEntity);
                bool isValidClaster = state.EntityManager.Exists(clasterEntity)
                    && state.EntityManager.HasComponent<Chunk>(clasterEntity)
                    && !state.EntityManager.HasComponent<RawChunk>(clasterEntity)
                    && !state.EntityManager.IsComponentEnabled<ThreadedChunk>(clasterEntity);

                if (isValidClaster) {
                    clasterEntities[i] = clasterEntity;
                    claster[i] = state.EntityManager.GetComponentData<Chunk>(clasterEntity).Voxels;
                } else if (!isValidClaster
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

            var job = new ChunkMeshDataJob {
                Entity = entity,
                Coordinate = chunk.Coordinate,
                Vertices = new NativeList<Vertex>(Allocator.Persistent),
                OpaqueIndices = new NativeList<ushort>(Allocator.Persistent),
                TransparentIndices = new NativeList<ushort>(Allocator.Persistent),
                ChunkCoordinate = chunk.Coordinate,
                Claster = claster,
                Blocks = SystemAPI.GetSingletonRW<BlockSystemData>().ValueRO.Blocks
            };

            var handle = job.Schedule();

            jobs.Add(new ScheduledJob {
                Data = job,
                Handle = handle
            });

            return true;
        }

        [BurstCompile]
        private bool TryCompleteJob(in ScheduledJob job, in RefRW<ChunkBufferingSystemData> chunkBufferingSystemData, in EntityCommandBuffer commandBuffer) {
            if (!job.Handle.IsCompleted) {
                return false;
            }

            job.Handle.Complete();

            commandBuffer.AddComponent(job.Data.Entity, new ChunkMeshData {
                Vertices = job.Data.Vertices.AsArray(),
                OpaqueIndices = job.Data.OpaqueIndices.AsArray(),
                TransparentIndices = job.Data.TransparentIndices.AsArray()
            });

            commandBuffer.SetComponentEnabled<DirtyChunk>(job.Data.Entity, false);

            var origin = job.Data.Coordinate - new int3(1, 1, 1);
            for (int i = 0; i < 3 * 3 * 3; i++) {
                var coordinate = origin + IndexUtility.ToCoordinate(i, 3, 3);
                ChunkBufferingSystem.GetEntity(chunkBufferingSystemData.ValueRO, coordinate, out Entity clasterEntity);
                if (clasterEntity != Entity.Null) {
                    commandBuffer.SetComponentEnabled<ThreadedChunk>(clasterEntity, false);
                }
            }

            job.Data.Dispose();

            return true;
        }

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state) {
            jobs = new NativeList<ScheduledJob>(Allocator.Persistent);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            var chunkBufferingSystemData = SystemAPI.GetSingletonRW<ChunkBufferingSystemData>();

            foreach (var (chunk, entity) in SystemAPI
                .Query<RefRO<Chunk>>()
                .WithAll<DirtyChunk, ImmediateChunk>()
                .WithNone<DisableRendering, ThreadedChunk>()
                .WithEntityAccess()) {

                if (!TryRun(ref state, chunk.ValueRO, entity, commandBuffer)) {
                    continue;
                }
            }

            foreach (var (chunk, entity) in SystemAPI
                .Query<RefRO<Chunk>>()
                .WithAll<DirtyChunk>()
                .WithNone<DisableRendering, ImmediateChunk, ThreadedChunk>()
                .WithEntityAccess()) {

                if (!TrySchedule(ref state, chunkBufferingSystemData, chunk.ValueRO, entity)) {
                    continue;
                }
            }

            for (int i = 0; i < jobs.Length; i++) {
                var job = jobs[i];

                if (TryCompleteJob(job, chunkBufferingSystemData, commandBuffer)) {
                    jobs.RemoveAt(i);
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