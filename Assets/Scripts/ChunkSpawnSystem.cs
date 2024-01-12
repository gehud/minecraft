using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Minecraft {
    [BurstCompile]
    public partial struct ChunkSpawnSystem : ISystem {
        public const int BatchSize = 16;

        private EntityQuery querry;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state) {
            querry = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ChunkSpawnRequest>()
                .Build(ref state);

            state.RequireForUpdate(querry);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            querry.ResetFilter();

            var entities = querry.ToEntityArray(Allocator.Temp);

            var count = math.min(entities.Length, BatchSize);
            for (int i = 0; i < count; i++) {
                InitializeChunk(ref state, entities[i]);
            }

            entities.Dispose();
        }

        [BurstCompile]
        private void InitializeChunk(ref SystemState state, in Entity entity) {
            var request = state.EntityManager.GetComponentData<ChunkSpawnRequest>(entity);

            if (!request.HasRenderer) {
                state.EntityManager.AddComponent<DisableRendering>(entity);
            }

            var position = request.Coordinate * Chunk.Size;

            state.EntityManager.AddComponentData(entity, new LocalToWorld {
                Value = float4x4.Translate(position)
            });

            var voxels = new NativeArray<Voxel>(Chunk.Volume, Allocator.Persistent);

            state.EntityManager.AddComponentData(entity, new Chunk {
                Coordinate = request.Coordinate,
                Voxels = voxels
            });

            state.EntityManager.SetName(entity, $"Chunk({request.Coordinate.x}, {request.Coordinate.y}, {request.Coordinate.z})");
            state.EntityManager.AddComponent<RawChunk>(entity);
            state.EntityManager.RemoveComponent<ChunkSpawnRequest>(entity);
        }
    }
}