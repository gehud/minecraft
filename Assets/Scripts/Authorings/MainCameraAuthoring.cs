using Minecraft.Components;
using Minecraft.Systems;
using Unity.Entities;
using UnityEngine;

namespace Minecraft.Authorings {
	[RequireComponent(typeof(Camera))]
	public class MainCameraAuthoring : MonoBehaviour {
		private void Awake() {
			var world = World.DefaultGameObjectInjectionWorld;
			var playerCameraSystem = world.GetExistingSystemManaged<PlayerCameraSystem>();
			var entityManager = world.EntityManager;
			entityManager.AddComponentData(playerCameraSystem.SystemHandle, new MainCamera {
				Camera = GetComponent<Camera>()
			});
		}
	}
}