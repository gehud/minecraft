using UnityEditor;
using UnityEngine;

namespace Minecraft.Player {
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour {
        [SerializeField, Min(0)]
        private float sencitivity = 5;

        [SerializeField]
        private MovementController movementController;
        [SerializeField]
        private float sneakHeight = 1.4f;
        [SerializeField]
        private float sprintFieldOfView = 70.0f;
        [SerializeField]
        private float fieldOfViewDelta = 1.0f;

        private new Camera camera;

        private float rotationX = 0.0f;
        private float rotationY = 0.0f;

        private float normalHeight;
        private float normalFieldOfView;
        private float targetFieldOfView = 1.0f;

		private void Awake() {
			camera = GetComponent<Camera>();
            normalHeight = transform.localPosition.y;
            normalFieldOfView = camera.fieldOfView;
		}

		private void Update() {
            if (movementController.IsSneaking) {
                transform.localPosition = Vector3.up * sneakHeight;
            } else {
                transform.localPosition = Vector3.up * normalHeight;
            }

            if (movementController.IsSprinting) {
                targetFieldOfView = sprintFieldOfView;
            } else {
                targetFieldOfView = normalFieldOfView;
            }

            camera.fieldOfView = Mathf.MoveTowards(camera.fieldOfView, targetFieldOfView, fieldOfViewDelta);

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            rotationX = Mathf.Clamp(rotationX - mouseY * sencitivity, -90.0f, 90.0f);
            rotationY += mouseX * sencitivity;
            transform.localEulerAngles = new Vector3(rotationX, rotationY, 0.0f);
        }
    }
}