using Minecraft.Components;
using Unity.Entities;
using UnityEngine;

namespace Minecraft.Authorings {
	public class PlayerCameraAuthoring : MonoBehaviour {
		public float Sensitivity = 10.0f;
		public GameObject OrientationTarget;

		private class Baker : Baker<PlayerCameraAuthoring> {
			public override void Bake(PlayerCameraAuthoring authoring) {
				var entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, new PlayerCamera {
					Sensitivity = authoring.Sensitivity,
					OrientationTarget = GetEntity(authoring.OrientationTarget, TransformUsageFlags.Dynamic)
				});
			}
		}
	}
}