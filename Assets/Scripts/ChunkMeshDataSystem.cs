using Minecraft.Lighting;
using Minecraft.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

namespace Minecraft {
    [BurstCompile]
    [UpdateAfter(typeof(LightingSystem))]
    public partial struct ChunkMeshDataSystem : ISystem {
        [BurstCompile]
        private bool TryRun(ref SystemState state, in Chunk chunk, in Entity entity, in EntityCommandBuffer commandBuffer) {
            var claster = new NativeArray<NativeArray<Voxel>>(3 * 3 * 3, Allocator.TempJob);
            var origin = chunk.Coordinate - new int3(1, 1, 1);
            var chunkBufferingSystemData = SystemAPI.GetSingletonRW<ChunkBufferingSystemData>();

            for (int i = 0; i < 3 * 3 * 3; i++) {
                var coordinate = origin + IndexUtility.ToCoordinate(i, 3, 3);
                ChunkBufferingSystem.GetEntity(chunkBufferingSystemData.ValueRO, coordinate, out Entity sideChunk);
                bool isValidChunk = state.EntityManager.Exists(sideChunk)
                    && state.EntityManager.HasComponent<Chunk>(sideChunk)
                    && !state.EntityManager.HasComponent<RawChunk>(sideChunk);

                if (isValidChunk) {
                    claster[i] = state.EntityManager.GetComponentData<Chunk>(sideChunk).Voxels;
                } else if (!isValidChunk 
                    && coordinate.y != -1
                    && coordinate.y != chunkBufferingSystemData.ValueRO.Height) {
                    claster.Dispose();
                    return false;
                }
            }

            var job = new ChunkMeshDataJob {
                Vertices = new NativeList<Vertex>(Allocator.Persistent),
                OpaqueIndices = new NativeList<ushort>(Allocator.Persistent),
                TransparentIndices = new NativeList<ushort>(Allocator.Persistent),
                ChunkCoordinate = chunk.Coordinate,
                Claster = claster,
                ChunkBufferingSystemData = SystemAPI.GetSingleton<ChunkBufferingSystemData>(),
                Blocks = SystemAPI.GetSingletonRW<BlockSystemData>().ValueRO.Blocks
            };

            job.Schedule().Complete();

            job.Claster.Dispose();

            commandBuffer.AddComponent(entity, new ChunkMeshData {
                Vertices = job.Vertices.AsArray(),
                OpaqueIndices = job.OpaqueIndices.AsArray(),
                TransparentIndices = job.TransparentIndices.AsArray()
            });

            commandBuffer.RemoveComponent<DirtyChunk>(entity);

            return true;
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (chunk, entity) in SystemAPI
                .Query<RefRO<Chunk>>()
                .WithAll<DirtyChunk, ImmediateChunk>()
                .WithNone<DisableRendering>()
                .WithEntityAccess()) {

                if (!TryRun(ref state, chunk.ValueRO, entity, commandBuffer)) {
                    continue;
                }

                commandBuffer.RemoveComponent<ImmediateChunk>(entity);
            }

            var batch = ChunkSpawnSystem.BatchSize;
            foreach (var (chunk, entity) in SystemAPI
                .Query<RefRO<Chunk>>()
                .WithAll<DirtyChunk>()
                .WithNone<DisableRendering, ImmediateChunk>()
                .WithEntityAccess()) {

                if (batch == 0) {
                    break;
                }

                if (TryRun(ref state, chunk.ValueRO, entity, commandBuffer)) {
                    --batch;
                }
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
}