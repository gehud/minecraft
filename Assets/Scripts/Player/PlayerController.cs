using UnityEngine;

namespace Minecraft.Player {
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(GroundChecker))]
    public class PlayerController : MonoBehaviour {
        [SerializeField]
        private CharacterController characterController;
        [SerializeField]
        private GroundChecker groundChecker;
        [SerializeField, Min(0)]
        private float speed = 5;
        [SerializeField, Min(0)]
        private float jumpingHeight = 1.125f;
        [SerializeField, Min(0)]
        private float gravity = 15;

        private Vector3 velocity = Vector3.zero;

        private void Update() {
            if (groundChecker.IsGrounded && Input.GetKeyDown(KeyCode.Space))
                Jump();
        }

        private void FixedUpdate() {
            characterController.Move(velocity * Time.fixedDeltaTime);

            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            Vector2 input = new(horizontalInput, verticalInput);
            input = input.magnitude > 1 ? input.normalized : input;

            velocity.x = input.x * speed;
            velocity.z = input.y * speed;

            if (groundChecker.IsGrounded) {
                velocity.y = 0;
            } else {
                velocity.y -= gravity * Time.fixedDeltaTime;
            }

            velocity = transform.TransformDirection(velocity);
        }

        private void Jump() {
            velocity.y += Mathf.Sqrt(2 * gravity * jumpingHeight);
        }
    }
}