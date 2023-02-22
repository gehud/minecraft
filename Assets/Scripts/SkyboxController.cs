using UnityEngine;

namespace Minecraft {
	public class SkyboxController : MonoBehaviour {
		[SerializeField]
		private new Transform camera;
		[SerializeField]
		private float cycleMinutes = 20.0f;
		[SerializeField]
		private float multiplier = 1.0f;

		private void LateUpdate() {
			if (camera != null) {
				transform.position = camera.position;
			} else {
				var main = Camera.main;
				if (main != null) {
					transform.position = Camera.main.transform.position;
				}
			}

			transform.Rotate(Vector3.forward, 360.0f / (cycleMinutes * 60.0f) * Time.deltaTime * multiplier);
		}
	}
}