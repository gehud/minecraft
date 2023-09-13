using Minecraft;
using Minecraft.Components;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Systems {
	public partial class PlayerInputSystem : SystemBase {
		private InputActions inputActions;

		protected override void OnCreate() {
			EntityManager.AddComponent<PlayerInput>(SystemHandle);
			inputActions = new();
			inputActions.Enable();
		}

		protected override void OnUpdate() {
			var playerInput = EntityManager.GetComponentDataRW<PlayerInput>(SystemHandle);
			playerInput.ValueRW.Movement = inputActions.Player.Move.ReadValue<Vector2>();
			playerInput.ValueRW.Look = inputActions.Player.Look.ReadValue<Vector2>();
			playerInput.ValueRW.Air = inputActions.Player.Air.ReadValue<float>();
		}
	}
}