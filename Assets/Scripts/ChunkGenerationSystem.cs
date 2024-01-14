using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Minecraft {
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(ChunkSpawnSystem))]
    public partial struct ChunkGenerationSystem : ISystem {
        private struct ScheduledJob {
            public ChunkGenerationJob Data;
            public JobHandle Handle;
        }

        private NativeList<ScheduledJob> jobs;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state) { 
            jobs = new NativeList<ScheduledJob>(Allocator.Persistent);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            var systemData = state.EntityManager.GetComponentDataRW<ChunkGenerationSystemData>(state.SystemHandle);

            foreach (var (chunk, entity) in SystemAPI
                .Query<RefRW<Chunk>>()
                .WithAll<RawChunk>()
                .WithNone<ThreadedChunk>()
                .WithEntityAccess()) {

                var job = new ChunkGenerationJob {
                    Entity = entity,
                    Coordinate = chunk.ValueRO.Coordinate,
                    Voxels = chunk.ValueRO.Voxels,
                    Continentalness = systemData.ValueRO.Continentalness,
                    Erosion = systemData.ValueRO.Erosion,
                    PeaksAndValleys = systemData.ValueRO.PeaksAndValleys,
                    WaterLevel = systemData.ValueRO.WaterLevel,
                };

                var handle = job.ScheduleParallel(Chunk.Volume, Chunk.Size, default);

                jobs.Add(new ScheduledJob {
                    Data = job,
                    Handle = handle
                });

                state.EntityManager.SetComponentEnabled<ThreadedChunk>(entity, true);
            }

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            for (int i = 0; i < jobs.Length; i++) {
                var job = jobs[i];

                if (TryCompleteJob(job, state.EntityManager, commandBuffer)) {
                    jobs.RemoveAt(i);
                }
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }

        [BurstCompile]
        private bool TryCompleteJob(in ScheduledJob job, in EntityManager entityManager, in EntityCommandBuffer commandBuffer) {
            if (!job.Handle.IsCompleted) {
                return false;
            }

            job.Handle.Complete();

            commandBuffer.RemoveComponent<RawChunk>(job.Data.Entity);
            entityManager.SetComponentEnabled<ThreadedChunk>(job.Data.Entity, false);

            return true;
        } 

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state) {
            jobs.Dispose();
        }
    }
}