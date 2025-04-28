using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Minecraft
{
    [BurstCompile]
    public partial struct ChunkSpawnSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var cmd = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (request, entity) in SystemAPI.Query<ChunkSpawnRequest>().WithEntityAccess())
            {
                if (!request.HasRenderer)
                {
                    cmd.AddComponent<DisableRendering>(entity);
                }

                var position = request.Coordinate * Chunk.Size;

                cmd.AddComponent(entity, new LocalToWorld
                {
                    Value = float4x4.Translate(position)
                });

                var voxels = new NativeArray<Voxel>(Chunk.Volume, Allocator.Persistent);

                cmd.AddComponent(entity, new Chunk
                {
                    Coordinate = request.Coordinate,
                    Voxels = voxels
                });

                cmd.SetName(entity, $"Chunk({request.Coordinate.x}, {request.Coordinate.y}, {request.Coordinate.z})");
                cmd.AddComponent<RawChunk>(entity);

                cmd.AddComponent<ThreadedChunk>(entity);
                cmd.SetComponentEnabled<ThreadedChunk>(entity, false);
                cmd.AddComponent<DirtyChunk>(entity);
                cmd.SetComponentEnabled<DirtyChunk>(entity, false);
                cmd.AddComponent<ImmediateChunk>(entity);
                cmd.SetComponentEnabled<ImmediateChunk>(entity, false);

                cmd.RemoveComponent<ChunkSpawnRequest>(entity);
            }
        }
    }
}
