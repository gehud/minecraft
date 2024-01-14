using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Minecraft {
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    public partial struct ChunkSpawnSystem : ISystem {
        public const int BatchSize = 16;

        private EntityQuery querry;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state) {
            querry = SystemAPI.QueryBuilder()
                .WithAll<ChunkSpawnRequest>()
                .Build();
        }

        [BurstCompile]
        private void Spawn(ref SystemState state, in EntityCommandBuffer commandBuffer, in Entity entity) {
            var request = state.EntityManager.GetComponentData<ChunkSpawnRequest>(entity);

            if (!request.HasRenderer) {
                commandBuffer.AddComponent<DisableRendering>(entity);
            }

            var position = request.Coordinate * Chunk.Size;

            commandBuffer.AddComponent(entity, new LocalToWorld {
                Value = float4x4.Translate(position)
            });

            var voxels = new NativeArray<Voxel>(Chunk.Volume, Allocator.Persistent);

            commandBuffer.AddComponent(entity, new Chunk {
                Coordinate = request.Coordinate,
                Voxels = voxels
            });

            commandBuffer.SetName(entity, $"Chunk({request.Coordinate.x}, {request.Coordinate.y}, {request.Coordinate.z})");
            commandBuffer.AddComponent<RawChunk>(entity);

            commandBuffer.AddComponent<ThreadedChunk>(entity);
            commandBuffer.SetComponentEnabled<ThreadedChunk>(entity, false);
            commandBuffer.AddComponent<DirtyChunk>(entity);
            commandBuffer.SetComponentEnabled<DirtyChunk>(entity, false);
            commandBuffer.AddComponent<ImmediateChunk>(entity);
            commandBuffer.SetComponentEnabled<ImmediateChunk>(entity, false);

            commandBuffer.RemoveComponent<ChunkSpawnRequest>(entity);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            var entities = querry.ToEntityArray(Allocator.Temp);

            var count = math.min(entities.Length, BatchSize);
            for (int i = 0; i < count; i++) {
                Spawn(ref state, commandBuffer, entities[i]);
            }

            entities.Dispose();

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
}