using UnityEngine;
using Zenject;

namespace Minecraft {

	public class FogController : MonoBehaviour {
		[SerializeField]
		private new Camera camera;

		[Inject]
		private readonly World world;

		private void LateUpdate() {
			float end = world.DrawDistance * Chunk.SIZE;
			camera.farClipPlane = end;
			RenderSettings.fogEndDistance = end;
		}
	}
}