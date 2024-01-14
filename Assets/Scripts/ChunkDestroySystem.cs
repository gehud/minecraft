using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Minecraft {
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    public partial struct ChunkDestroySystem : ISystem {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (_, entity) in SystemAPI
                .Query<RefRO<ChunkToDestroy>>()
                .WithNone<ThreadedChunk>()
                .WithEntityAccess()) {

                ChunkBufferingSystem.DestroyChunk(state.EntityManager, commandBuffer, entity);
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
}