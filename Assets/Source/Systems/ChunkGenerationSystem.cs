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
            public int3 Coordinate;
            [WriteOnly]
            public NativeArray<Voxel> Voxels;

            public void Execute(int index) {
                var localCoordinate = IndexUtility.ToCoordinate(index, Chunk.Size, Chunk.Size);
                var coordinate = Coordinate * Chunk.Size + localCoordinate;
                var noiseCoordinate = new float2 {
                    x = coordinate.x / 500.0f,
                    y = coordinate.z / 500.0f
                };

                int height = (int)math.floor(noise.snoise(noiseCoordinate) * 32);
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

            for (int i = 0; i < entities.Length; i++) {
                var entity = entities[i];

                if (entity == lastEntity) {
                    continue;
                }

                var chunk = EntityManager.GetComponentData<Chunk>(entity);
                lastJob = new ChunkGenerationJob {
                    Entity = entity,
                    Coordinate = chunk.Coordinate,
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