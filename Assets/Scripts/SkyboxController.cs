using UnityEngine;
using Zenject;

namespace Minecraft {
	public class SkyboxController : MonoInstaller {
		public float Time {
			get => transform.eulerAngles.z / 15.0f;
			set => transform.rotation = Quaternion.Euler(0.0f, 0.0f, value * 15.0f);
		}

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

			transform.Rotate(Vector3.forward, 360.0f / (cycleMinutes * 60.0f) * UnityEngine.Time.deltaTime * multiplier);
		}

		public override void InstallBindings() {
			Container.Bind<SkyboxController>().FromInstance(this);
		}
	}
}