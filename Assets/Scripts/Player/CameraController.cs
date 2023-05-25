using Minecraft.UI;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Minecraft.Player {
    public class CameraController : NetworkBehaviour {

		[SerializeField, Min(0)]
        private float sencitivity = 5;

        public float FOV {
            get => normalFOV;
            set => normalFOV = value;
		}

        [SerializeField]
        private MovementController movementController;
        [SerializeField]
        private float sneakHeight = 1.4f;
        [SerializeField]
        private float sprintFOVMultiplier = 1.5f;
        [SerializeField]
        private float FOVDelta = 1.0f;
        [SerializeField]
        private Renderer bodyRenderer;
        [SerializeField]
        private Renderer handRenderer;
        [SerializeField]
		private new Camera camera;
        [SerializeField]
        private Camera skyboxCamera;

        private IInputProvider inputProvider;

        private float rotationX = 0.0f;
        private float rotationY = 0.0f;

        private float normalHeight;
        private float normalFOV;
        private float targetFOV = 1.0f;

        [Inject]
        private readonly UIController uIController;

        [Inject]
        private readonly ISavePayload savePayload;

		private void Awake() {
            inputProvider = GetComponent<IInputProvider>();
            normalHeight = camera.transform.localPosition.y;
            normalFOV = camera.fieldOfView;
		}

		private void Start() {
			Cursor.lockState = CursorLockMode.Locked;
		}

		public override void OnNetworkSpawn() {
			base.OnNetworkSpawn();
            if (!IsOwner) {
                bodyRenderer.gameObject.layer = 0;
                handRenderer.gameObject.layer = LayerMask.NameToLayer("Player");
				Destroy(camera.GetComponent<AudioListener>());
                camera.enabled = false;
                skyboxCamera.enabled = false;
			}
		}

		private void Update() {
			if (savePayload.Role != ConnectionRoles.None && !IsOwner)
				return;

			if (movementController.IsSneaking) {
                camera.transform.localPosition = Vector3.up * sneakHeight;
            } else {
				camera.transform.localPosition = Vector3.up * normalHeight;
            }

            if (movementController.IsSprinting) {
                targetFOV = normalFOV * sprintFOVMultiplier;
            } else {
                targetFOV = normalFOV;
            }

            camera.fieldOfView = Mathf.MoveTowards(camera.fieldOfView, targetFOV, FOVDelta);

            if (!uIController.IsUsing) {
                var input = inputProvider.Look;

                rotationX = Mathf.Clamp(rotationX - input.y * sencitivity, -90.0f, 90.0f);
                rotationY += input.x * sencitivity;
                camera.transform.localEulerAngles = new Vector3(rotationX, rotationY, 0.0f);
            }
        }
	}
}