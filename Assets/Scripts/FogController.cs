using UnityEngine;
using Zenject;

namespace Minecraft {
	public class FogController : MonoBehaviour {
		[SerializeField]
		private new Camera camera;
		[SerializeField]
		private float fogStartOffset = 16.0f;
		[SerializeField]
		private float fogEndOffset = 8.0f;
		[SerializeField, Min(0.0f)]
		private float drawDistanceOffset = 8.0f;

		[Inject]
		private readonly World world;

		private void LateUpdate() {
			var drawDistance = world.DrawDistance * Chunk.SIZE;
			camera.farClipPlane = drawDistance + drawDistanceOffset;
			RenderSettings.fogStartDistance = drawDistance - fogStartOffset;
			RenderSettings.fogEndDistance = drawDistance + fogEndOffset;
		}
	}
}