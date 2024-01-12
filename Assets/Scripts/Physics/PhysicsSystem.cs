using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Minecraft.Physics {
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct PhysicsSystem : ISystem {
        public static readonly float3 Gravity = new(0.0f, -15.0f, 0.0f);
        public static readonly float ContactOffset = 0.08f;

        [BurstCompile]
        public static bool Raycast(in BlockSystemData blockSystemData, in EntityManager entityManager, in ChunkBufferingSystemData chunkBufferingSystemData, in Ray ray, float maxDistance, out RaycastHit raycastHit) {
            var xOrigin = ray.origin.x;
            var yOrigin = ray.origin.y;
            var zOrigin = ray.origin.z;

            var xDirection = ray.direction.x;
            var yDirectoin = ray.direction.y;
            var zDirection = ray.direction.z;

            var time = 0.0f;
            var xCoordinate = math.floor(xOrigin);
            var yCoordinate = math.floor(yOrigin);
            var zCoordinate = math.floor(zOrigin);

            var xStep = xDirection > 0.0f ? 1.0f : -1.0f;
            var yStep = yDirectoin > 0.0f ? 1.0f : -1.0f;
            var zStep = zDirection > 0.0f ? 1.0f : -1.0f;

            var xDelta = xDirection == 0.0f ? float.PositiveInfinity : math.abs(1.0f / xDirection);
            var yDelta = yDirectoin == 0.0f ? float.PositiveInfinity : math.abs(1.0f / yDirectoin);
            var zDelta = zDirection == 0.0f ? float.PositiveInfinity : math.abs(1.0f / zDirection);

            var xDistance = xStep > 0.0f ? xCoordinate + 1.0f - xOrigin : xOrigin - xCoordinate;
            var yDistance = yStep > 0.0f ? yCoordinate + 1.0f - yOrigin : yOrigin - yCoordinate;
            var zDistance = zStep > 0.0f ? zCoordinate + 1.0f - zOrigin : zOrigin - zCoordinate;

            var xMax = xDelta < float.PositiveInfinity ? xDelta * xDistance : float.PositiveInfinity;
            var yMax = yDelta < float.PositiveInfinity ? yDelta * yDistance : float.PositiveInfinity;
            var zMax = zDelta < float.PositiveInfinity ? zDelta * zDistance : float.PositiveInfinity;

            int steppedIndex = -1;

            Vector3 endPosition;
            Vector3 endCoordinate;
            Vector3 normal;

            while (time <= maxDistance) {
                var voxelCoordinate = new int3((int)xCoordinate, (int)yCoordinate, (int)zCoordinate);
                ChunkBufferingSystem.GetVoxel(chunkBufferingSystemData, entityManager, voxelCoordinate, out var voxel);
                var block = (int)voxel.Type;
                if (blockSystemData.Blocks[block].IsSolid) {
                    endPosition.x = xOrigin + time * xDirection;
                    endPosition.y = yOrigin + time * yDirectoin;
                    endPosition.z = zOrigin + time * zDirection;

                    endCoordinate.x = xCoordinate;
                    endCoordinate.y = yCoordinate;
                    endCoordinate.z = zCoordinate;

                    normal.x = normal.y = normal.z = 0.0f;

                    if (steppedIndex == 0) {
                        normal.x = -xStep;
                    }

                    if (steppedIndex == 1) {
                        normal.y = -yStep;
                    }

                    if (steppedIndex == 2) {
                        normal.z = -zStep;
                    }

                    raycastHit = new() {
                        point = endCoordinate,
                        normal = normal
                    };

                    return true;
                }

                if (xMax < yMax) {
                    if (xMax < zMax) {
                        xCoordinate += xStep;
                        time = xMax;
                        xMax += xDelta;
                        steppedIndex = 0;
                    } else {
                        zCoordinate += zStep;
                        time = zMax;
                        zMax += zDelta;
                        steppedIndex = 2;
                    }
                } else {
                    if (yMax < zMax) {
                        yCoordinate += yStep;
                        time = yMax;
                        yMax += yDelta;
                        steppedIndex = 1;
                    } else {
                        zCoordinate += zStep;
                        time = zMax;
                        zMax += zDelta;
                        steppedIndex = 2;
                    }
                }
            }

            endCoordinate.x = xCoordinate;
            endCoordinate.y = yCoordinate;
            endCoordinate.z = zCoordinate;

            endPosition.x = xOrigin + time * xDirection;
            endPosition.y = yOrigin + time * yDirectoin;
            endPosition.z = zOrigin + time * zDirection;
            normal.x = normal.y = normal.z = 0.0f;

            raycastHit = new() {
                point = endCoordinate,
                normal = normal
            };

            return false;
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            float deltaTime = state.WorldUnmanaged.Time.DeltaTime;

            var blocks = SystemAPI.GetSingleton<BlockSystemData>().Blocks;
            var chunkBufferingSystemData = SystemAPI.GetSingleton<ChunkBufferingSystemData>();

            foreach (var (hitbox, transform) in SystemAPI
                .Query<RefRW<Hitbox>, RefRW<LocalTransform>>()
                .WithAll<Simulate>().WithNone<Disabled>()) {

                var extents = hitbox.ValueRO.Bounds.Extents;
                var offset = hitbox.ValueRO.Bounds.Center;

                if (!hitbox.ValueRO.DisableGravity) {
                    hitbox.ValueRW.Velocity += Gravity * deltaTime;
                }

                transform.ValueRW.Position += hitbox.ValueRW.Velocity * deltaTime;

                if (hitbox.ValueRW.Velocity.x < 0.0f) {
                    int x = (int)math.floor(transform.ValueRW.Position.x + offset.x - extents.x - ContactOffset);
                    for (int y = (int)math.floor(transform.ValueRW.Position.y + offset.y - extents.y + ContactOffset); y <= (int)math.floor(transform.ValueRW.Position.y + offset.y + extents.y - ContactOffset); y++) {
                        for (int z = (int)math.floor(transform.ValueRW.Position.z + offset.z - extents.z + ContactOffset); z <= (int)math.floor(transform.ValueRW.Position.z + offset.z + extents.z - ContactOffset); z++) {
                            ChunkBufferingSystem.GetVoxel(chunkBufferingSystemData, state.EntityManager, new int3(x, y, z), out var voxel);
                            if (blocks[(int)voxel.Type].IsSolid) {
                                hitbox.ValueRW.Velocity = new float3(0.0f, hitbox.ValueRW.Velocity.y, hitbox.ValueRW.Velocity.z);
                                transform.ValueRW.Position = new float3(x + 1.0f - offset.x + extents.x + ContactOffset, transform.ValueRW.Position.y, transform.ValueRW.Position.z);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.x > 0.0f) {
                    int x = (int)math.floor(transform.ValueRW.Position.x + offset.x + extents.x + ContactOffset);
                    for (int y = (int)math.floor(transform.ValueRW.Position.y + offset.y - extents.y + ContactOffset); y <= (int)math.floor(transform.ValueRW.Position.y + offset.y + extents.y - ContactOffset); y++) {
                        for (int z = (int)math.floor(transform.ValueRW.Position.z + offset.z - extents.z + ContactOffset); z <= (int)math.floor(transform.ValueRW.Position.z + offset.z + extents.z - ContactOffset); z++) {
                            ChunkBufferingSystem.GetVoxel(chunkBufferingSystemData, state.EntityManager, new int3(x, y, z), out var voxel);
                            if (blocks[(int)voxel.Type].IsSolid) {
                                hitbox.ValueRW.Velocity = new float3(0.0f, hitbox.ValueRW.Velocity.y, hitbox.ValueRW.Velocity.z);
                                transform.ValueRW.Position = new float3(x - offset.x - extents.x - ContactOffset, transform.ValueRW.Position.y, transform.ValueRW.Position.z);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.z < 0.0f) {
                    int z = (int)math.floor(transform.ValueRW.Position.z + offset.z - extents.z - ContactOffset);
                    for (int y = (int)math.floor(transform.ValueRW.Position.y + offset.y - extents.y + ContactOffset); y <= (int)math.floor(transform.ValueRW.Position.y + offset.y + extents.y - ContactOffset); y++) {
                        for (int x = (int)math.floor(transform.ValueRW.Position.x + offset.x - extents.x + ContactOffset); x <= (int)math.floor(transform.ValueRW.Position.x + offset.x + extents.x - ContactOffset); x++) {
                            ChunkBufferingSystem.GetVoxel(chunkBufferingSystemData, state.EntityManager, new int3(x, y, z), out var voxel);
                            if (blocks[(int)voxel.Type].IsSolid) {
                                hitbox.ValueRW.Velocity = new float3(hitbox.ValueRW.Velocity.x, hitbox.ValueRW.Velocity.y, 0.0f);
                                transform.ValueRW.Position = new float3(transform.ValueRW.Position.x, transform.ValueRW.Position.y, z + 1.0f - offset.z + extents.z + ContactOffset);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.z > 0.0f) {
                    int z = (int)math.floor(transform.ValueRW.Position.z + offset.z + extents.z + ContactOffset);
                    for (int y = (int)math.floor(transform.ValueRW.Position.y + offset.y - extents.y + ContactOffset); y <= (int)math.floor(transform.ValueRW.Position.y + offset.y + extents.y - ContactOffset); y++) {
                        for (int x = (int)math.floor(transform.ValueRW.Position.x + offset.x - extents.x + ContactOffset); x <= (int)math.floor(transform.ValueRW.Position.x + offset.x + extents.x - ContactOffset); x++) {
                            ChunkBufferingSystem.GetVoxel(chunkBufferingSystemData, state.EntityManager, new int3(x, y, z), out var voxel);
                            if (blocks[(int)voxel.Type].IsSolid) {
                                hitbox.ValueRW.Velocity = new float3(hitbox.ValueRW.Velocity.x, hitbox.ValueRW.Velocity.y, 0.0f);
                                transform.ValueRW.Position = new float3(transform.ValueRW.Position.x, transform.ValueRW.Position.y, z - offset.z - extents.z - ContactOffset);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.y < 0.0f) {
                    int y = (int)math.floor(transform.ValueRW.Position.y + offset.y - extents.y - ContactOffset);
                    for (int x = (int)math.floor(transform.ValueRW.Position.x + offset.x - extents.x + ContactOffset); x <= (int)math.floor(transform.ValueRW.Position.x + offset.x + extents.x - ContactOffset); x++) {
                        for (int z = (int)math.floor(transform.ValueRW.Position.z + offset.z - extents.z + ContactOffset); z <= (int)math.floor(transform.ValueRW.Position.z + offset.z + extents.z - ContactOffset); z++) {
                            ChunkBufferingSystem.GetVoxel(chunkBufferingSystemData, state.EntityManager, new int3(x, y, z), out var voxel);
                            if (blocks[(int)voxel.Type].IsSolid) {
                                hitbox.ValueRW.Velocity = new float3(hitbox.ValueRW.Velocity.x, 0.0f, hitbox.ValueRW.Velocity.z);
                                transform.ValueRW.Position = new float3(transform.ValueRW.Position.x, y + 1.0f - offset.y + extents.y + ContactOffset, transform.ValueRW.Position.z);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.y > 0.0f) {
                    int y = (int)math.floor(transform.ValueRW.Position.y + offset.y + extents.y + ContactOffset);
                    for (int x = (int)math.floor(transform.ValueRW.Position.x + offset.x - extents.x + ContactOffset); x <= (int)math.floor(transform.ValueRW.Position.x + offset.x + extents.x - ContactOffset); x++) {
                        for (int z = (int)math.floor(transform.ValueRW.Position.z + offset.z - extents.z + ContactOffset); z <= (int)math.floor(transform.ValueRW.Position.z + offset.z + extents.z - ContactOffset); z++) {
                            ChunkBufferingSystem.GetVoxel(chunkBufferingSystemData, state.EntityManager, new int3(x, y, z), out var voxel);
                            if (blocks[(int)voxel.Type].IsSolid) {
                                hitbox.ValueRW.Velocity = new float3(hitbox.ValueRW.Velocity.x, 0.0f, hitbox.ValueRW.Velocity.z);
                                transform.ValueRW.Position = new float3(transform.ValueRW.Position.x, y - offset.y - extents.y - ContactOffset, transform.ValueRW.Position.z);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}