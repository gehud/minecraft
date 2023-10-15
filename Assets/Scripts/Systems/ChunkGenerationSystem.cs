using Minecraft.Components;
using Minecraft.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Minecraft.Systems {
    [UpdateAfter(typeof(ChunkSystem))]
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
                var localCoordinate = Array3DUtility.To3D(index, Chunk.SIZE, Chunk.SIZE);
                var coordinate = Coordinate * Chunk.SIZE + localCoordinate;
                var noiseCoordinate = new float2 {
                    x = coordinate.x / 500.0f,
                    y = coordinate.z / 500.0f
                };

                int height = (int)math.floor(noise.snoise(noiseCoordinate) * 32);
                if (coordinate.y <= height) {
                    Voxels[index] = new Voxel(BlockType.Stone);
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
                EntityManager.AddComponent<DirtyChunk>(lastEntity);
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
                    Voxels = new NativeArray<Voxel>(Chunk.VOLUME, Allocator.TempJob)
                };

                lastJobHandle = lastJob.Schedule(Chunk.VOLUME, Chunk.SIZE);

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