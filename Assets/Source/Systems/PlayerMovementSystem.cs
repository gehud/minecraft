using Minecraft.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Minecraft.Systems {
    [UpdateAfter(typeof(PlayerInputSystem))]
    [UpdateAfter(typeof(PlayerCameraSystem))]
    public partial class PlayerMovementSystem : SystemBase {
        protected override void OnUpdate() {
            var playerInput = SystemAPI.GetSingleton<PlayerInput>();

            Entities.ForEach((ref Hitbox hitbox, in PlayerMovement movement) => {
                var orientation = EntityManager.GetComponentData<LocalToWorld>(movement.OrientationSource);

                var velocity = hitbox.Velocity;

                var translation = orientation.Forward * playerInput.Movement.y
                    + orientation.Right * playerInput.Movement.x;

                translation *= movement.Speed * (playerInput.IsSprint ? 1.5f : 1.0f);

                velocity.z = translation.z;
                velocity.x = translation.x;

                if (playerInput.IsJump) {
                    velocity -= math.sign(PhysicsSystem.Gravity) * math.sqrt(2.0f * movement.JumpHeight * math.abs(PhysicsSystem.Gravity));
                }

                hitbox.Velocity = velocity;
            }).WithoutBurst().Run();
        }
    }
}