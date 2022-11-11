using UnityEngine;

namespace Minecraft.Player {
    public class CameraRotation : MonoBehaviour {
        [SerializeField, Min(0)]
        private float sencitivity = 5;

        private float rotationX = 0.0f;
        private float rotationY = 0.0f;

        private void Update() {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            rotationX = Mathf.Clamp(rotationX - mouseY * sencitivity, -90.0f, 90.0f);
            rotationY += mouseX * sencitivity;
            transform.localEulerAngles = new Vector3(rotationX, rotationY, 0.0f);
        }
    }
}