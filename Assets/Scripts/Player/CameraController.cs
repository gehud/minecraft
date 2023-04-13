using Minecraft.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zenject;

namespace Minecraft.Player {
    [RequireComponent(typeof(Camera))]
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
        private Transform body;
        [SerializeField]
		private new Camera camera;
        [SerializeField]
        private Camera skyboxCamera;

        private float rotationX = 0.0f;
        private float rotationY = 0.0f;

        private float normalHeight;
        private float normalFOV;
        private float targetFOV = 1.0f;

        private Vector3 bodyRotation;

        [Inject]
        private readonly UIController uIController;

        [Inject]
        private readonly ISavePayload savePayload;

		private void Awake() {
            normalHeight = transform.localPosition.y;
            normalFOV = camera.fieldOfView;
            bodyRotation = body.eulerAngles;
		}

		private void Start() {
			Cursor.lockState = CursorLockMode.Locked;
		}

		public override void OnNetworkSpawn() {
			base.OnNetworkSpawn();
            if (!IsOwner) {
                body.gameObject.layer = LayerMask.GetMask("Default");
                Destroy(GetComponent<AudioListener>());
                camera.enabled = false;
                skyboxCamera.enabled = false;
			}
		}

		private void Update() {
			if (savePayload.Role != ConnectionRoles.None && !IsOwner)
				return;

			if (movementController.IsSneaking) {
                transform.localPosition = Vector3.up * sneakHeight;
            } else {
                transform.localPosition = Vector3.up * normalHeight;
            }

            if (movementController.IsSprinting) {
                targetFOV = normalFOV * sprintFOVMultiplier;
            } else {
                targetFOV = normalFOV;
            }

            camera.fieldOfView = Mathf.MoveTowards(camera.fieldOfView, targetFOV, FOVDelta);

            if (!uIController.IsUsing) { 
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                rotationX = Mathf.Clamp(rotationX - mouseY * sencitivity, -90.0f, 90.0f);
                rotationY += mouseX * sencitivity;
                transform.localEulerAngles = new Vector3(rotationX, rotationY, 0.0f);
                body.rotation = Quaternion.Euler(bodyRotation.x, transform.eulerAngles.y, bodyRotation.z);
            }
        }
	}
}