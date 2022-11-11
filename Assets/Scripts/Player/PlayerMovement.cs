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
            if (groundChecker.IsGrounded && Input.GetKeyDown(KeyCode.Space))
                Jump();
        }

        private void FixedUpdate() {
            float velocityY = rigidbody.velocity.y;
            rigidbody.velocity = new Vector3(velocity.x, velocityY, velocity.z);

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