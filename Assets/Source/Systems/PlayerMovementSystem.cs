using Minecraft.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Minecraft.Systems {
    [UpdateAfter(typeof(PlayerInputSystem))]
    [UpdateAfter(typeof(PlayerCameraSystem))]
    public partial class PlayerMovementSystem : SystemBase {
        private SystemHandle playerInputSystem;

        protected override void OnCreate() {
            playerInputSystem = World.GetExistingSystem<PlayerInputSystem>();
        }

        protected override void OnUpdate() {
            var playerInput = EntityManager.GetComponentDataRW<PlayerInput>(playerInputSystem);

            Entities.ForEach((ref Hitbox hitbox, in PlayerMovement movement) => {
                var orientation = EntityManager.GetComponentData<LocalToWorld>(movement.OrientationSource);

                var velocity = hitbox.Velocity;

                var translation = orientation.Forward * playerInput.ValueRO.Movement.y
                    + orientation.Right * playerInput.ValueRO.Movement.x;

                translation *= movement.Speed * (playerInput.ValueRO.IsSprint ? 1.5f : 1.0f);

                velocity.z = translation.z;
                velocity.x = translation.x;

                if (playerInput.ValueRO.IsJump) {
                    velocity.y += math.sqrt(2 * movement.JumpHeight * 9.81f);
                }

                hitbox.Velocity = velocity;
            }).WithoutBurst().Run();
        }
    }
}