using UnityEngine;
using Zenject;

namespace Minecraft {

	public class FogController : MonoBehaviour {
		[SerializeField]
		private new Camera camera;

		[Inject]
		private readonly World world;

		private void LateUpdate() {
			camera.farClipPlane = Mathf.Max(World.HEIGHT * Chunk.SIZE, world.DrawDistance * Chunk.SIZE);
			RenderSettings.fogEndDistance = world.DrawDistance * Chunk.SIZE;
		}
	}
}