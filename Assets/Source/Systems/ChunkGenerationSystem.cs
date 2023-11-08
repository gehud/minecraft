using Minecraft.Components;
using Minecraft.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Minecraft.Systems {
    [UpdateAfter(typeof(ChunkInitializationSystem))]
    public partial class ChunkGenerationSystem : SystemBase {
        private ChunkGenerationJob lastJob;
        private JobHandle lastJobHandle;

        private struct ChunkGenerationJob : IJobParallelFor {
            [ReadOnly]
            public Entity Entity;
            [ReadOnly]
            public int HeightOffset;
            [ReadOnly]
            public int3 Coordinate;
            [ReadOnly]
            public Noise Continentalness;
            [ReadOnly]
            public Noise Erosion;
            [ReadOnly]
            public Noise PeaksAndValleys;
            [WriteOnly]
            public NativeArray<Voxel> Voxels;

            public void Execute(int index) {
                var localCoordinate = IndexUtility.ToCoordinate(index, Chunk.Size, Chunk.Size);
                var coordinate = Coordinate * Chunk.Size + localCoordinate;

                var continentalness = Continentalness.Sample2D(coordinate.x, coordinate.z);
                var erosion = Erosion.Sample2D(coordinate.x, coordinate.z);
                var peaksAndValleys = PeaksAndValleys.Sample2D(coordinate.x, coordinate.z);
                var result = continentalness * erosion * peaksAndValleys;

                int height = (int)result + HeightOffset;
                if (coordinate.y <= height) {
                    if (coordinate.y == height) {
                        Voxels[index] = new Voxel(BlockType.Grass);
                    } else if (coordinate.y >= height - 4) {
                        Voxels[index] = new Voxel(BlockType.Dirt);
                    } else {
                        Voxels[index] = new Voxel(BlockType.Stone);
                    }
                }
            }
        }

        private void ScheduleSingleJob(NativeArray<Entity> entities) {
            if (!lastJobHandle.IsCompleted) {
                return;
            }

            lastJobHandle.Complete();

            var lastEntity = lastJob.Entity;

            if (EntityManager.Exists(lastEntity)) {
                var chunk = EntityManager.GetComponentData<Chunk>(lastEntity);
                lastJob.Voxels.CopyTo(chunk.Voxels);
                EntityManager.SetComponentData(lastEntity, chunk);
                EntityManager.RemoveComponent<RawChunk>(lastEntity);
            }

            if (lastJob.Voxels.IsCreated) {
                lastJob.Voxels.Dispose();
            }

            lastJob = default;

            var chunkBufferingSystemData = SystemAPI.GetSingleton<ChunkBufferingSystemData>();
            var systemData = EntityManager.GetComponentDataRW<ChunkGenerationSystemData>(SystemHandle);

            for (int i = 0; i < entities.Length; i++) {
                var entity = entities[i];

                if (entity == lastEntity) {
                    continue;
                }

                var chunk = EntityManager.GetComponentData<Chunk>(entity);
                lastJob = new ChunkGenerationJob {
                    Entity = entity,
                    HeightOffset = systemData.ValueRO.HeightOffset,
                    Coordinate = chunk.Coordinate,
                    Continentalness = systemData.ValueRO.Continentalness,
                    Erosion = systemData.ValueRO.Erosion,
                    PeaksAndValleys = systemData.ValueRO.PeaksAndValleys,
                    Voxels = new NativeArray<Voxel>(Chunk.Volume, Allocator.Persistent)
                };

                lastJobHandle = lastJob.Schedule(Chunk.Volume, Chunk.Size);

                return;
            }
        }

        protected override void OnUpdate() {
            var querry = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RawChunk>()
                .WithAll<Chunk>()
                .Build(EntityManager);

            var entities = querry.ToEntityArray(Allocator.Temp);
            querry.Dispose();

            ScheduleSingleJob(entities);

            entities.Dispose();
        }
    }
}