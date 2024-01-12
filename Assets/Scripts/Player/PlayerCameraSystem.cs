using Minecraft.Lighting;
using Minecraft.Physics;
using Minecraft.UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Minecraft.Player {
    [UpdateAfter(typeof(PlayerInputSystem))]
    public partial class PlayerCameraSystem : SystemBase {
        protected override void OnUpdate() {
            var camera = Camera.main;
            if (!camera) {
                return;
            }

            var playerInput = SystemAPI.GetSingleton<PlayerInput>();

            var chunkBufferingSystemData = SystemAPI.GetSingleton<ChunkBufferingSystemData>();
            var blockSystemData = SystemAPI.GetSingleton<BlockSystemData>();
            var lightingSystemData = SystemAPI.GetSingleton<LightingSystemData>();

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            Entities.ForEach((ref LocalTransform localTransform, ref PlayerCamera playerCamera, in LocalToWorld transform) => {
                camera.transform.position = transform.Position;

                if (Cursor.lockState != CursorLockMode.Locked) {
                    return;
                }

                camera.transform.rotation = transform.Rotation;

                playerCamera.Yaw += playerInput.Look.x * playerCamera.Sensitivity;
                playerCamera.Pitch += -playerInput.Look.y * playerCamera.Sensitivity;
                playerCamera.Pitch = math.clamp(playerCamera.Pitch, -90.0f, 90.0f);
                localTransform.Rotation.value = math.mul(float4x4.RotateY(math.radians(playerCamera.Yaw)),
                    float4x4.RotateX(math.radians(playerCamera.Pitch))).Rotation().value;

                var orientation = EntityManager.GetComponentData<LocalTransform>(playerCamera.OrientationTarget);
                orientation.Rotation = quaternion.RotateY(math.radians(playerCamera.Yaw));
                EntityManager.SetComponentData(playerCamera.OrientationTarget, orientation);

                var ray = new Ray {
                    origin = transform.Position,
                    direction = localTransform.Forward()
                };

                if (playerInput.IsAttack) {
                    if (PhysicsSystem.Raycast(blockSystemData, EntityManager, chunkBufferingSystemData, ray, 15.0f, out var hitInfo)) {
                        var voxelCoordinate = (int3)math.floor(hitInfo.point);
                        ChunkBufferingSystem.DestroyVoxel(chunkBufferingSystemData, blockSystemData, lightingSystemData, EntityManager, commandBuffer, voxelCoordinate);
                    }
                } else if (playerInput.IsDefend && Hotbar.Selected && Hotbar.Selected is BlockView blockView) {
                    if (PhysicsSystem.Raycast(blockSystemData, EntityManager, chunkBufferingSystemData, ray, 15.0f, out var hitInfo)) {
                        var voxelCoordinate = (int3)math.floor(hitInfo.point + hitInfo.normal);
                        var blockType = blockView.BlockType;
                        ChunkBufferingSystem.PlaceVoxel(chunkBufferingSystemData, blockSystemData, lightingSystemData, EntityManager, commandBuffer, voxelCoordinate, blockType);
                    }
                }
            }).WithStructuralChanges().Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}