using System;
using UnityEngine;
using Zenject;

namespace Minecraft.Player {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(GroundChecker))]
    public class MovementController : MonoBehaviour {
        public bool IsSneaking => isSneaking;

        public bool IsSprinting => isSprinting;

        [SerializeField]
        private new Transform camera;

        [SerializeField, Min(0)]
        private float walkSpeed = 5;
        [SerializeField, Min(0)]
        private float sneakSpeed = 3;
        [SerializeField, Min(0)]
        private float sprintSpeed = 7;
        [SerializeField, Min(0)]
        private float speedDelta = 1.0f;

        [SerializeField, Min(0)]
        private float jumpingHeight = 1.125f;
        [SerializeField, Min(0)]
        private float doubleTapTime = 0.5f;

        [Header("Keys")]
        [SerializeField]
        private KeyCode jumpKey = KeyCode.Space;
        [SerializeField]
        private KeyCode sneakKey = KeyCode.LeftShift;
        [SerializeField]
        private KeyCode sprintKey = KeyCode.LeftControl;

        private float lastDoubleTapTime = 0.0f;

        private new Rigidbody rigidbody;
        private BoxCollider boxCollider;
        private GroundChecker groundChecker;
        private Vector3 velocity = Vector3.zero;
        private float targetSpeed = 0;
        private float speed = 0;
        private bool isSneaking = false;
        private bool isSprinting = false;

        [Inject]
        private World World { get; }

        [Inject]
        private BlockDataManager BlockDataManager { get; }

        private void Awake() {
            rigidbody = GetComponent<Rigidbody>();
            boxCollider = GetComponent<BoxCollider>();
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

			float horizontalInput = Input.GetAxis("Horizontal");
			float verticalInput = Input.GetAxis("Vertical");
			Vector2 input = new(horizontalInput, verticalInput);
			input = input.magnitude > 1 ? input.normalized : input;

            isSneaking = Input.GetKey(sneakKey) && isGrounded;
            isSprinting = Input.GetKeyDown(sprintKey) || isSprinting && input.magnitude == 1.0f;

			if (isSneaking) {
                targetSpeed = sneakSpeed;
            } else if (isSprinting) {
                targetSpeed = sprintSpeed;
            } else {
                targetSpeed = walkSpeed;
            }

            if (Input.GetKeyDown(jumpKey)) {
                if (isGrounded)
                    Jump();
                if (Time.time - lastDoubleTapTime < doubleTapTime)
                    rigidbody.useGravity = !rigidbody.useGravity;
                lastDoubleTapTime = Time.time;
            }

            speed = Mathf.MoveTowards(speed, targetSpeed, speedDelta);

            if (rigidbody.useGravity) {
                velocity.y = rigidbody.velocity.y;
            } else {
                velocity.y = Input.GetAxis("Fly") * speed;
            }

            velocity.x = input.x * speed;
            velocity.z = input.y * speed;
            velocity = Quaternion.Euler(0, camera.localEulerAngles.y, 0) * velocity;

            if (isSneaking && velocity != Vector3.zero) {
                var direction = velocity.normalized;
                if (direction.x > 0.0f)
                    direction.x = 1.0f;
                else if (direction.x < 0.0f)
                    direction.x = -1.0f;
                if (direction.z > 0.0f)
                    direction.z = 1.0f;
                else if (direction.z < 0.0f)
                    direction.z = -1.0f;

                var extents = Vector3.one * boxCollider.bounds.extents.x;

                if (!CheckSquare(transform.position + Vector3.forward * direction.z * speed * Time.fixedDeltaTime, extents)) {
                    velocity.z = 0.0f;
                }

                if (!CheckSquare(transform.position + Vector3.right * direction.x * speed * Time.fixedDeltaTime, extents)) {
                    velocity.x = 0.0f;
                }
            }

            rigidbody.velocity = velocity;
        }

        private bool CheckSquare(Vector3 position, Vector3 extents) {
            for (int x = Mathf.FloorToInt(position.x - extents.x); x <= Mathf.FloorToInt(position.x + extents.x); x++) {
                for (int z = Mathf.FloorToInt(position.z - extents.z); z <= Mathf.FloorToInt(position.z + extents.z); z++) {
                    if (BlockDataManager.Data[World.GetBlock(new Vector3Int(x, Mathf.FloorToInt(position.y - extents.y), z))].IsSolid)
                        return true;
                }
            }

            return false;
        }
    }
}