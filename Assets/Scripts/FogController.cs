using UnityEngine;
using Zenject;

namespace Minecraft {
	public class FogController : MonoBehaviour {
		[Inject]
		private readonly World world;

		[SerializeField]
		private float fogStartOffset = 16.0f;
		[SerializeField]
		private float fogEndOffset = 8.0f;
		[SerializeField, Min(0.0f)]
		private float drawDistanceOffset = 8.0f;

		private new Camera camera;

		public void OnPlayerSpawn(GameObject player) {
			camera = player.GetComponentInChildren<Camera>();
		}

		private void LateUpdate() {
			if (camera == null)
				return;

			var drawDistance = world.DrawDistance * Chunk.SIZE;
			camera.farClipPlane = drawDistance + drawDistanceOffset;
			RenderSettings.fogStartDistance = drawDistance - fogStartOffset;
			RenderSettings.fogEndDistance = drawDistance + fogEndOffset;
		}

		private void OnEnable() {
			PlayerEvents.OnSpawn += OnPlayerSpawn;
		}

		private void OnDisable() {
			PlayerEvents.OnSpawn -= OnPlayerSpawn;
		}
	}
}