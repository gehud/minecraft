using Assets.Scripts.Systems;
using Minecraft.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Minecraft.Systems {
	public partial class PlayerCameraSystem : SystemBase {
		private SystemHandle playerInputSystem;

		protected override void OnCreate() {
			playerInputSystem = World.GetExistingSystem<PlayerInputSystem>();
		}

		protected override void OnUpdate() {
			var camera = EntityManager.GetComponentObject<MainCamera>(SystemHandle).Camera;
			var playerInput = EntityManager.GetComponentDataRW<PlayerInput>(playerInputSystem);

			Entities.ForEach((ref LocalTransform localTransform, ref PlayerCamera playerCamera, in LocalToWorld transform) => {
				camera.transform.position = transform.Position;
				camera.transform.rotation = transform.Rotation;

				playerCamera.Yaw += playerInput.ValueRO.Look.x * playerCamera.Sensitivity * World.Time.DeltaTime;
				playerCamera.Pitch += -playerInput.ValueRO.Look.y * playerCamera.Sensitivity * World.Time.DeltaTime;
				playerCamera.Pitch = math.clamp(playerCamera.Pitch, -90.0f, 90.0f);
				localTransform.Rotation.value = math.mul(float4x4.RotateY(playerCamera.Yaw), 
					float4x4.RotateX(playerCamera.Pitch)).Rotation().value;
				
				var orientation = EntityManager.GetComponentData<LocalTransform>(playerCamera.OrientationTarget);
				orientation.Rotation = quaternion.RotateY(playerCamera.Yaw);
				EntityManager.SetComponentData(playerCamera.OrientationTarget, orientation);
			}).WithoutBurst().Run();
		}
	}
}