using Minecraft.Components;
using Minecraft.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

namespace Minecraft.Systems {
    [BurstCompile]
    [UpdateAfter(typeof(LightingSystem))]
    public partial struct ChunkMeshDataSystem : ISystem {
        private ChunkMeshDataJob lastJob;
        private JobHandle lastJobHandle;

        [BurstCompile]
        private void ScheduleSinsgleJob(ref SystemState state, in NativeArray<Entity> entities) {
            if (!lastJobHandle.IsCompleted) {
                return;
            }

            lastJobHandle.Complete();

            var lastEntity = lastJob.Entity;

            if (lastJob.Claster.IsCreated) {
                lastJob.Claster.Dispose();
            }

            if (state.EntityManager.Exists(lastEntity)) {
                state.EntityManager.AddComponentData(lastEntity, new ChunkMeshData {
                    Vertices = lastJob.Vertices.AsArray(),
                    Indices = lastJob.Indices.AsArray()
                });

                state.EntityManager.RemoveComponent<DirtyChunk>(lastEntity);
            } else {
                lastJob.Vertices.Dispose();
                lastJob.Indices.Dispose();
            }

            lastJob = default;

            for (int i = 0; i < entities.Length; i++) {
                var entity = entities[i];

                if (entity == lastEntity) {
                    continue;
                }

                var claster = new NativeArray<NativeArray<Voxel>>(3 * 3 * 3, Allocator.TempJob);
                var chunkCoordinate = state.EntityManager.GetComponentData<Chunk>(entity).Coordinate;
                var origin = chunkCoordinate - new int3(1, 1, 1);
                var chunkBufferingSystemData = SystemAPI.GetSingletonRW<ChunkBufferingSystemData>();
                bool isValidClaster = true;
                for (int j = 0; j < 3 * 3 * 3; j++) {
                    var coordinate = origin + IndexUtility.ToCoordinate(j, 3, 3);
                    ChunkBufferingSystem.GetEntity(chunkBufferingSystemData.ValueRO, coordinate, out Entity chunk);
                    bool isValidChunk = state.EntityManager.Exists(chunk) && state.EntityManager.HasComponent<Chunk>(chunk) && !state.EntityManager.HasComponent<RawChunk>(chunk);

                    if (isValidChunk) {
                        claster[j] = state.EntityManager.GetComponentData<Chunk>(chunk).Voxels;
                    } else if (coordinate.y != -1 && coordinate.y != chunkBufferingSystemData.ValueRO.Height && !isValidChunk) {
                        isValidClaster = false;
                        break;
                    }
                }

                if (!isValidClaster) {
                    claster.Dispose();
                    continue;
                }

                lastJob = new ChunkMeshDataJob {
                    Vertices = new NativeList<Vertex>(Allocator.Persistent),
                    Indices = new NativeList<ushort>(Allocator.Persistent),
                    ChunkCoordinate = chunkCoordinate,
                    Entity = entity,
                    Claster = claster,
                    Blocks = SystemAPI.GetSingletonRW<BlockSystemData>().ValueRO.Blocks
                };

                lastJobHandle = lastJob.Schedule();

                return;
            }
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (chunk, entity) in SystemAPI
                .Query<RefRO<Chunk>>()
                .WithAll<ImmediateChunk>()
                .WithAll<DirtyChunk>()
                .WithNone<DisableRendering>()
                .WithEntityAccess()) {
                var claster = new NativeArray<NativeArray<Voxel>>(3 * 3 * 3, Allocator.TempJob);
                var chunkCoordinate = state.EntityManager.GetComponentData<Chunk>(entity).Coordinate;
                var origin = chunkCoordinate - new int3(1, 1, 1);
                var chunkBufferingSystemData = SystemAPI.GetSingletonRW<ChunkBufferingSystemData>();

                for (int j = 0; j < 3 * 3 * 3; j++) {
                    var coordinate = origin + IndexUtility.ToCoordinate(j, 3, 3);
                    ChunkBufferingSystem.GetEntity(chunkBufferingSystemData.ValueRO, coordinate, out Entity sideChunk);
                    bool isValidChunk = state.EntityManager.Exists(sideChunk) && state.EntityManager.HasComponent<Chunk>(sideChunk) && !state.EntityManager.HasComponent<RawChunk>(sideChunk);

                    if (isValidChunk) {
                        claster[j] = state.EntityManager.GetComponentData<Chunk>(sideChunk).Voxels;
                    } else if (coordinate.y != -1 && coordinate.y != chunkBufferingSystemData.ValueRO.Height && !isValidChunk) {
                        claster.Dispose();
                        return;
                    }
                }

                var job = new ChunkMeshDataJob {
                    Blocks = SystemAPI.GetSingletonRW<BlockSystemData>().ValueRO.Blocks,
                    ChunkCoordinate = chunk.ValueRO.Coordinate,
                    Indices = new NativeList<ushort>(Allocator.Persistent),
                    Vertices = new NativeList<Vertex>(Allocator.Persistent),
                    Claster = claster,
                };

                job.Schedule().Complete();

                job.Claster.Dispose();

                commandBuffer.AddComponent(entity, new ChunkMeshData {
                    Vertices = job.Vertices.AsArray(),
                    Indices = job.Indices.AsArray()
                });

                commandBuffer.RemoveComponent<DirtyChunk>(entity);
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();

            var querry = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Chunk>()
                .WithAll<DirtyChunk>()
                .WithNone<DisableRendering>()
                .WithNone<ImmediateChunk>()
                .Build(state.EntityManager);

            var entities = querry.ToEntityArray(Allocator.Temp);
            querry.Dispose();

            ScheduleSinsgleJob(ref state, entities);

            entities.Dispose();
        }
    }
}