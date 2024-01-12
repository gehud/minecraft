using Unity.Entities;
using UnityEngine;

namespace Minecraft.Player {
    public partial class PlayerInputSystem : SystemBase {
        private Controls controls;

        protected override void OnCreate() {
            EntityManager.AddComponent<PlayerInput>(SystemHandle);
            controls = new();
            controls.Enable();
        }

        protected override void OnUpdate() {
            var playerInput = EntityManager.GetComponentDataRW<PlayerInput>(SystemHandle);
            playerInput.ValueRW.Movement = controls.Player.Move.ReadValue<Vector2>();
            playerInput.ValueRW.Look = controls.Player.Look.ReadValue<Vector2>();
            playerInput.ValueRW.Air = controls.Player.Air.ReadValue<float>();
            playerInput.ValueRW.IsJump = controls.Player.Jump.WasPressedThisFrame();
            playerInput.ValueRW.IsSprint = controls.Player.Sprint.ReadValue<float>() > 0.0f;

            playerInput.ValueRW.IsAttack = controls.Player.Attack.WasPressedThisFrame();
            playerInput.ValueRW.IsDefend = controls.Player.Defend.WasPressedThisFrame();
        }
    }
}