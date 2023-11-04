using Minecraft.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Minecraft.Systems {
    [BurstCompile]
    public partial struct ChunkInitializationSystem : ISystem {
        [BurstCompile]
        private void SetupChunk(ref SystemState state, in NativeArray<Entity> entities) {
            if (entities.Length == 0) {
                return;
            }

            var entity = entities[0];

            var chunkInitializer = state.EntityManager.GetComponentData<ChunkInitializer>(entity);

            if (!chunkInitializer.HasRenderer) {
                state.EntityManager.AddComponent<DisableRendering>(entity);
            }

            var position = chunkInitializer.Coordinate * Chunk.Size;

            state.EntityManager.AddComponentData(entity, new LocalToWorld {
                Value = float4x4.Translate(position)
            });

            var voxels = new NativeArray<Voxel>(Chunk.Volume, Allocator.Persistent);

            state.EntityManager.AddComponentData(entity, new Chunk {
                Coordinate = chunkInitializer.Coordinate,
                Voxels = voxels
            });

            state.EntityManager.SetName(entity, $"Chunk({chunkInitializer.Coordinate.x}, {chunkInitializer.Coordinate.y}, {chunkInitializer.Coordinate.z})");
            state.EntityManager.AddComponent<RawChunk>(entity);
            state.EntityManager.RemoveComponent<ChunkInitializer>(entity);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            var querry = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ChunkInitializer>()
                .Build(state.EntityManager);
            var entities = querry.ToEntityArray(Allocator.Temp);
            querry.Dispose();

            SetupChunk(ref state, entities);

            entities.Dispose();
        }
    }
}