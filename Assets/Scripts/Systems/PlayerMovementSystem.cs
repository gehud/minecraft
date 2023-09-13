using Assets.Scripts.Systems;
using Minecraft.Components;
using Unity.Entities;
using Unity.Transforms;

namespace Minecraft.Systems {
	[UpdateAfter(typeof(PlayerCameraSystem))]
	public partial class PlayerMovementSystem : SystemBase {
		private SystemHandle playerInputSystem;

		protected override void OnCreate() {
			playerInputSystem = World.GetExistingSystem<PlayerInputSystem>();
		}

		protected override void OnUpdate() {
			var playerInput = EntityManager.GetComponentDataRW<PlayerInput>(playerInputSystem);

			Entities.ForEach((ref LocalTransform transform, in PlayerMovement movement) => {
				var orientation = EntityManager.GetComponentData<LocalToWorld>(movement.OrientationSource);

				var translation = (orientation.Forward * playerInput.ValueRO.Movement.y +
					orientation.Right * playerInput.ValueRO.Movement.x + 
					orientation.Up * playerInput.ValueRO.Air) * 
						movement.Speed * World.Time.DeltaTime;

				transform = transform.Translate(translation);
			}).WithoutBurst().Run();
		}
	}
}