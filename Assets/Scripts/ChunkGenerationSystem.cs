using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Minecraft {
    [BurstCompile]
    [UpdateAfter(typeof(ChunkSpawnSystem))]
    public partial struct ChunkGenerationSystem : ISystem {
        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state) {
            state.RequireForUpdate<RawChunk>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            var systemData = state.EntityManager.GetComponentDataRW<ChunkGenerationSystemData>(state.SystemHandle);

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (chunk, entity) in SystemAPI
                .Query<RefRW<Chunk>>()
                .WithAll<RawChunk>()
                .WithEntityAccess()) {

                var job = new ChunkGenerationJob {
                    Coordinate = chunk.ValueRO.Coordinate,
                    Continentalness = systemData.ValueRO.Continentalness,
                    Erosion = systemData.ValueRO.Erosion,
                    PeaksAndValleys = systemData.ValueRO.PeaksAndValleys,
                    WaterLevel = systemData.ValueRO.WaterLevel,
                    Voxels = chunk.ValueRO.Voxels
                };

                job.ScheduleParallel(Chunk.Volume, Chunk.Size, default).Complete();

                commandBuffer.RemoveComponent<RawChunk>(entity);
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
}