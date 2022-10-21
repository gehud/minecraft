using UnityEngine;

namespace Minecraft.Player
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        private new Camera camera;
        [SerializeField]
        private Transform body;
        [SerializeField, Min(0)] 
        private float sencitivity = 5;

        private float roatationX = 0;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            roatationX = Mathf.Clamp(roatationX - mouseY * sencitivity, -90, 90);
            transform.localEulerAngles = new Vector3(roatationX, 0, 0);
            body.Rotate(Vector3.up, mouseX * sencitivity);
        }
    }
}