using System.Collections;
using UnityEngine;

namespace Minecraft.Player {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GroundChecker))]
    public class PlayerMovement : MonoBehaviour {
        [SerializeField] 
        private new Transform camera;
        [SerializeField, Min(0)]
        private float speed = 5;
        [SerializeField, Min(0)]
        private float jumpingHeight = 1.125f;
        [SerializeField, Min(0)]
        private float doubleTapTime = 0.5f;

        private float lastDoubleTapTime = 0.0f;

        private new Rigidbody rigidbody;
        private IGroundChecker groundChecker;
        private Vector3 velocity = Vector3.zero;

        private void Awake() {
            rigidbody = GetComponent<Rigidbody>();
            groundChecker = GetComponent<GroundChecker>();
        }

        private void Jump() {
            float velocity = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * jumpingHeight);
            rigidbody.velocity += Vector3.up * velocity;
        }

        private void Update() {
            bool isGrounded = groundChecker.IsGrounded;

            if (isGrounded) {
                if (!rigidbody.useGravity)
                    rigidbody.useGravity = true;
            }

            if (Input.GetKeyDown(KeyCode.Space)) {
                if (isGrounded)
                    Jump();
                if (Time.time - lastDoubleTapTime < doubleTapTime)
                    rigidbody.useGravity = !rigidbody.useGravity;
                lastDoubleTapTime = Time.time;
            }
        }

        private void FixedUpdate() {
            if (rigidbody.useGravity)
                velocity.y = rigidbody.velocity.y;
            else
                velocity.y = Input.GetAxis("Fly") * speed;

            rigidbody.velocity = velocity;

            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            Vector2 input = new(horizontalInput, verticalInput);
            input = input.magnitude > 1 ? input.normalized : input;

            velocity.x = input.x * speed;
            velocity.z = input.y * speed;

            velocity = Quaternion.Euler(0, camera.localEulerAngles.y, 0) * velocity;
        }
    }
}