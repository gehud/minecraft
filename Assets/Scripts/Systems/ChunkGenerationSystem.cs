using Minecraft.Components;
using Minecraft.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Systems {
    [BurstCompile]
    [UpdateAfter(typeof(ChunkSystem))]
    public partial struct ChunkGenerationSystem : ISystem {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (chunk, entity) in SystemAPI.
                Query<RefRW<Chunk>>().
                WithAll<RawChunk>().
                WithEntityAccess()) {

                for (int i = 0; i < Chunk.VOLUME; i++) {
                    var localCoordinate = Array3DUtility.To3D(i, Chunk.SIZE, Chunk.SIZE);
                    var coordinate = chunk.ValueRO.Coordinate * Chunk.SIZE + localCoordinate;
                    var noiseCoordinate = new float2 {
                        x = coordinate.x / 500.0f,
                        y = coordinate.z / 500.0f
                    };

                    int height = (int)math.floor(noise.snoise(noiseCoordinate) * 32);
                    if (coordinate.y <= height) {
                        chunk.ValueRW.Voxels[i] = new Voxel(BlockType.Stone);
                    }
                }

                commandBuffer.RemoveComponent<RawChunk>(entity);
                commandBuffer.AddComponent<DirtyChunk>(entity);
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
}