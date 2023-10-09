using Minecraft.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Minecraft.Systems {
    public partial class PlayerCameraSystem : SystemBase {
        private SystemHandle playerInputSystem;

        protected override void OnCreate() {
            playerInputSystem = World.GetExistingSystem<PlayerInputSystem>();
            Cursor.lockState = CursorLockMode.Locked;
        }

        protected override void OnUpdate() {
            var camera = EntityManager.GetComponentObject<MainCamera>(SystemHandle).Camera;
            var playerInput = EntityManager.GetComponentDataRW<PlayerInput>(playerInputSystem);

            Entities.ForEach((ref LocalTransform localTransform, ref PlayerCamera playerCamera, in LocalToWorld transform) => {
                camera.transform.position = transform.Position;
                camera.transform.rotation = transform.Rotation;

                playerCamera.Yaw += playerInput.ValueRO.Look.x * playerCamera.Sensitivity;
                playerCamera.Pitch += -playerInput.ValueRO.Look.y * playerCamera.Sensitivity;
                playerCamera.Pitch = math.clamp(playerCamera.Pitch, -90.0f, 90.0f);
                localTransform.Rotation.value = math.mul(float4x4.RotateY(math.radians(playerCamera.Yaw)),
                    float4x4.RotateX(math.radians(playerCamera.Pitch))).Rotation().value;

                var orientation = EntityManager.GetComponentData<LocalTransform>(playerCamera.OrientationTarget);
                orientation.Rotation = quaternion.RotateY(math.radians(playerCamera.Yaw));
                EntityManager.SetComponentData(playerCamera.OrientationTarget, orientation);
            }).WithoutBurst().Run();
        }
    }
}