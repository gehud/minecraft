using Minecraft.Components;
using Unity.Entities;
using UnityEngine;

namespace Minecraft.Authorings {
	public class PlayerMovementAuthoring : MonoBehaviour {
		public float Speed = 5.0f;
		public GameObject OrientationSource;

		private class Baker : Baker<PlayerMovementAuthoring> {
			public override void Bake(PlayerMovementAuthoring authoring) {
				var entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, new PlayerMovement {
					Speed = authoring.Speed,
					OrientationSource = GetEntity(authoring.OrientationSource, TransformUsageFlags.Dynamic)
				});
			}
		}
	}
}