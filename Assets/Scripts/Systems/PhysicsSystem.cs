using Minecraft.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Minecraft.Systems {
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct PhysicsSystem : ISystem {
        private float3 gravity;
        private float contactOffset;

        private static SystemHandle chunkBufferingSystem;

        public static bool Raycast(EntityManager entityManager, Ray ray, float maxDistance, out RaycastHit raycastHit) {
            float px = ray.origin.x;
            float py = ray.origin.y;
            float pz = ray.origin.z;

            float dx = ray.direction.x;
            float dy = ray.direction.y;
            float dz = ray.direction.z;

            float t = 0.0f;
            float ix = Mathf.Floor(px);
            float iy = Mathf.Floor(py);
            float iz = Mathf.Floor(pz);

            float stepx = dx > 0.0f ? 1.0f : -1.0f;
            float stepy = dy > 0.0f ? 1.0f : -1.0f;
            float stepz = dz > 0.0f ? 1.0f : -1.0f;

            float infinity = float.PositiveInfinity;

            float txDelta = dx == 0.0f ? infinity : Mathf.Abs(1.0f / dx);
            float tyDelta = dy == 0.0f ? infinity : Mathf.Abs(1.0f / dy);
            float tzDelta = dz == 0.0f ? infinity : Mathf.Abs(1.0f / dz);

            float xdist = stepx > 0 ? ix + 1 - px : px - ix;
            float ydist = stepy > 0 ? iy + 1 - py : py - iy;
            float zdist = stepz > 0 ? iz + 1 - pz : pz - iz;

            float txMax = txDelta < infinity ? txDelta * xdist : infinity;
            float tyMax = tyDelta < infinity ? tyDelta * ydist : infinity;
            float tzMax = tzDelta < infinity ? tzDelta * zdist : infinity;

            int steppedIndex = -1;

            Vector3 end;
            Vector3 iend;
            Vector3 norm;

            while (t <= maxDistance) {
                var chunkBuffer = entityManager.GetComponentData<ChunkBuffer>(chunkBufferingSystem);
                var block = (int)ChunkBufferingSystem.GetVoxel(entityManager, chunkBuffer, new int3((int)ix, (int)iy, (int)iz)).Type;
                if (StaticBlockDatabase.Data[block].IsSolid) {
                    end.x = px + t * dx;
                    end.y = py + t * dy;
                    end.z = pz + t * dz;

                    iend.x = ix;
                    iend.y = iy;
                    iend.z = iz;

                    norm.x = norm.y = norm.z = 0.0f;
                    if (steppedIndex == 0)
                        norm.x = -stepx;
                    if (steppedIndex == 1)
                        norm.y = -stepy;
                    if (steppedIndex == 2)
                        norm.z = -stepz;

                    raycastHit = new() {
                        point = iend,
                        normal = norm
                    };

                    return true;
                }

                if (txMax < tyMax) {
                    if (txMax < tzMax) {
                        ix += stepx;
                        t = txMax;
                        txMax += txDelta;
                        steppedIndex = 0;
                    } else {
                        iz += stepz;
                        t = tzMax;
                        tzMax += tzDelta;
                        steppedIndex = 2;
                    }
                } else {
                    if (tyMax < tzMax) {
                        iy += stepy;
                        t = tyMax;
                        tyMax += tyDelta;
                        steppedIndex = 1;
                    } else {
                        iz += stepz;
                        t = tzMax;
                        tzMax += tzDelta;
                        steppedIndex = 2;
                    }
                }
            }

            iend.x = ix;
            iend.y = iy;
            iend.z = iz;

            end.x = px + t * dx;
            end.y = py + t * dy;
            end.z = pz + t * dz;
            norm.x = norm.y = norm.z = 0.0f;

            raycastHit = new() {
                point = iend,
                normal = norm
            };

            return false;
        }

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