using UnityEditor;
using UnityEngine;

namespace Minecraft.Player {
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour {
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

        private new Camera camera;

        private float rotationX = 0.0f;
        private float rotationY = 0.0f;

        private float normalHeight;
        private float normalFOV;
        private float targetFOV = 1.0f;

        private bool isInMenu = false;

		private void Awake() {
			camera = GetComponent<Camera>();
            normalHeight = transform.localPosition.y;
            normalFOV = camera.fieldOfView;
		}

		private void Start() {
			Cursor.lockState = CursorLockMode.Locked;
		}

		private void Update() {
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

            if (!isInMenu) { 
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                rotationX = Mathf.Clamp(rotationX - mouseY * sencitivity, -90.0f, 90.0f);
                rotationY += mouseX * sencitivity;
                transform.localEulerAngles = new Vector3(rotationX, rotationY, 0.0f);
            }
        }

        private void OnMenuEnter() {
            isInMenu = true;
        }

		private void OnMenuExit() {
			isInMenu = false;
		}

		private void OnEnable() {
            MenuController.OnEnter += OnMenuEnter;
            MenuController.OnExit += OnMenuExit;
		}

		private void OnDisable() {
			MenuController.OnEnter -= OnMenuEnter;
			MenuController.OnExit -= OnMenuExit;
		}
	}
}