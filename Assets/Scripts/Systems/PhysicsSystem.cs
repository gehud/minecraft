using Minecraft.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Minecraft.Systems {
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct PhysicsSystem : ISystem {
        private float3 gravity;
        private float contactOffset;

        private SystemHandle chunkBufferingSystem;

        void ISystem.OnCreate(ref SystemState state) {
            gravity = new(0.0f, -9.81f, 0.0f);
            contactOffset = 0.08f;
            chunkBufferingSystem = state.WorldUnmanaged.GetExistingSystemState<ChunkBufferingSystem>().SystemHandle;
        }

        void ISystem.OnUpdate(ref SystemState state) {
            float deltaTime = state.WorldUnmanaged.Time.DeltaTime;

            var data = StaticBlockDatabase.Data;
            var chunkBuffer = state.EntityManager.GetComponentData<ChunkBuffer>(chunkBufferingSystem);

            foreach (var (hitbox, transform) in SystemAPI
                .Query<RefRW<Hitbox>, RefRW<LocalTransform>>()) {

                var extents = hitbox.ValueRO.Bounds.Extents;
                var offset = hitbox.ValueRO.Bounds.Center;

                transform.ValueRW.Position += hitbox.ValueRW.Velocity * deltaTime;

                if (hitbox.ValueRW.Velocity.x < 0.0f) {
                    int x = (int)math.floor(transform.ValueRW.Position.x + offset.x - extents.x - contactOffset);
                    for (int y = (int)math.floor(transform.ValueRW.Position.y + offset.y - extents.y + contactOffset); y <= (int)math.floor(transform.ValueRW.Position.y + offset.y + extents.y - contactOffset); y++) {
                        for (int z = (int)math.floor(transform.ValueRW.Position.z + offset.z - extents.z + contactOffset); z <= (int)math.floor(transform.ValueRW.Position.z + offset.z + extents.z - contactOffset); z++) {
                            if (data[(int)ChunkBufferingSystem.GetVoxel(state.EntityManager, chunkBuffer, new int3(x, y, z)).Type].IsSolid) {
                                hitbox.ValueRW.Velocity = new float3(0.0f, hitbox.ValueRW.Velocity.y, hitbox.ValueRW.Velocity.z);
                                transform.ValueRW.Position = new float3(x + 1.0f - offset.x + extents.x + contactOffset, transform.ValueRW.Position.y, transform.ValueRW.Position.z);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.x > 0.0f) {
                    int x = (int)math.floor(transform.ValueRW.Position.x + offset.x + extents.x + contactOffset);
                    for (int y = (int)math.floor(transform.ValueRW.Position.y + offset.y - extents.y + contactOffset); y <= (int)math.floor(transform.ValueRW.Position.y + offset.y + extents.y - contactOffset); y++) {
                        for (int z = (int)math.floor(transform.ValueRW.Position.z + offset.z - extents.z + contactOffset); z <= (int)math.floor(transform.ValueRW.Position.z + offset.z + extents.z - contactOffset); z++) {
                            if (data[(int)ChunkBufferingSystem.GetVoxel(state.EntityManager, chunkBuffer, new int3(x, y, z)).Type].IsSolid) {
                                hitbox.ValueRW.Velocity = new float3(0.0f, hitbox.ValueRW.Velocity.y, hitbox.ValueRW.Velocity.z);
                                transform.ValueRW.Position = new float3(x - offset.x - extents.x - contactOffset, transform.ValueRW.Position.y, transform.ValueRW.Position.z);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.z < 0.0f) {
                    int z = (int)math.floor(transform.ValueRW.Position.z + offset.z - extents.z - contactOffset);
                    for (int y = (int)math.floor(transform.ValueRW.Position.y + offset.y - extents.y + contactOffset); y <= (int)math.floor(transform.ValueRW.Position.y + offset.y + extents.y - contactOffset); y++) {
                        for (int x = (int)math.floor(transform.ValueRW.Position.x + offset.x - extents.x + contactOffset); x <= (int)math.floor(transform.ValueRW.Position.x + offset.x + extents.x - contactOffset); x++) {
                            if (data[(int)ChunkBufferingSystem.GetVoxel(state.EntityManager, chunkBuffer, new int3(x, y, z)).Type].IsSolid) {
                                hitbox.ValueRW.Velocity = new float3(hitbox.ValueRW.Velocity.x, hitbox.ValueRW.Velocity.y, 0.0f);
                                transform.ValueRW.Position = new float3(transform.ValueRW.Position.x, transform.ValueRW.Position.y, z + 1.0f - offset.z + extents.z + contactOffset);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.z > 0.0f) {
                    int z = (int)math.floor(transform.ValueRW.Position.z + offset.z + extents.z + contactOffset);
                    for (int y = (int)math.floor(transform.ValueRW.Position.y + offset.y - extents.y + contactOffset); y <= (int)math.floor(transform.ValueRW.Position.y + offset.y + extents.y - contactOffset); y++) {
                        for (int x = (int)math.floor(transform.ValueRW.Position.x + offset.x - extents.x + contactOffset); x <= (int)math.floor(transform.ValueRW.Position.x + offset.x + extents.x - contactOffset); x++) {
                            if (data[(int)ChunkBufferingSystem.GetVoxel(state.EntityManager, chunkBuffer, new int3(x, y, z)).Type].IsSolid) {
                                hitbox.ValueRW.Velocity = new float3(hitbox.ValueRW.Velocity.x, hitbox.ValueRW.Velocity.y, 0.0f);
                                transform.ValueRW.Position = new float3(transform.ValueRW.Position.x, transform.ValueRW.Position.y, z - offset.z - extents.z - contactOffset);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.y < 0.0f) {
                    int y = (int)math.floor(transform.ValueRW.Position.y + offset.y - extents.y - contactOffset);
                    for (int x = (int)math.floor(transform.ValueRW.Position.x + offset.x - extents.x + contactOffset); x <= (int)math.floor(transform.ValueRW.Position.x + offset.x + extents.x - contactOffset); x++) {
                        for (int z = (int)math.floor(transform.ValueRW.Position.z + offset.z - extents.z + contactOffset); z <= (int)math.floor(transform.ValueRW.Position.z + offset.z + extents.z - contactOffset); z++) {
                            if (data[(int)ChunkBufferingSystem.GetVoxel(state.EntityManager, chunkBuffer, new int3(x, y, z)).Type].IsSolid) {
                                hitbox.ValueRW.Velocity = new float3(hitbox.ValueRW.Velocity.x, 0.0f, hitbox.ValueRW.Velocity.z);
                                transform.ValueRW.Position = new float3(transform.ValueRW.Position.x, y + 1.0f - offset.y + extents.y + contactOffset, transform.ValueRW.Position.z);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.y > 0.0f) {
                    int y = (int)math.floor(transform.ValueRW.Position.y + offset.y + extents.y + contactOffset);
                    for (int x = (int)math.floor(transform.ValueRW.Position.x + offset.x - extents.x + contactOffset); x <= (int)math.floor(transform.ValueRW.Position.x + offset.x + extents.x - contactOffset); x++) {
                        for (int z = (int)math.floor(transform.ValueRW.Position.z + offset.z - extents.z + contactOffset); z <= (int)math.floor(transform.ValueRW.Position.z + offset.z + extents.z - contactOffset); z++) {
                            if (data[(int)ChunkBufferingSystem.GetVoxel(state.EntityManager, chunkBuffer, new int3(x, y, z)).Type].IsSolid) {
                                hitbox.ValueRW.Velocity = new float3(hitbox.ValueRW.Velocity.x, 0.0f, hitbox.ValueRW.Velocity.z);
                                transform.ValueRW.Position = new float3(transform.ValueRW.Position.x, y - offset.y - extents.y - contactOffset, transform.ValueRW.Position.z);
                                break;
                            }
                        }
                    }
                }

                if (!hitbox.ValueRO.DisableGravity) {
                    hitbox.ValueRW.Velocity += gravity * deltaTime;
                }
            }
        }
    }
}