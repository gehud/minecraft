using Minecraft.Utilities;
using System;
using UnityEngine;
using Zenject;

namespace Minecraft.Player {
    [RequireComponent(typeof(BoundsAuthoring))]
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

        private BoundsAuthoring bounds;
        private Vector3 velocity = Vector3.zero;
        private float targetSpeed = 0;
        private float speed = 0;
        private bool isSneaking = false;
        private bool isSprinting = false;

        [Inject]
        private World World { get; }

        [Inject]
        private BlockDataProvider BlockDataProvider { get; }

        [Inject]
        private PhysicsSolver PhysicsSolver { get; }

        private Hitbox hitbox;

        private void Awake() {
            for (int y = World.HEIGHT * Chunk.SIZE; y >= 0; --y) {
                if (BlockDataProvider.Get(World.GetBlock(new Vector3Int(0, y, 0))).IsSolid) {
                    transform.position = new Vector3(0.0f, y + 1.0f, 0.0f);
                    break;
                }
            }
			bounds = GetComponent<BoundsAuthoring>();
            hitbox = PhysicsSolver.CreatePlayer();
            hitbox.Position = transform.position;
            hitbox.Bounds = bounds.Value;
        }

		private void Jump() {
            float velocity = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * jumpingHeight);
            hitbox.Velocity += Vector3.up * velocity;
        }

        private void Update() {
			transform.position = hitbox.Position;

            bool isGrounded = BlockDataProvider.Get(World.GetBlock(CoordinateUtility.ToCoordinate(transform.position + Vector3.down))).IsSolid;

            if (isGrounded) {
                if (!hitbox.UseGravity)
                    hitbox.UseGravity = true;
            }

            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            Vector2 input = new(horizontalInput, verticalInput);
            input = input.magnitude > 1 ? input.normalized : input;

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
                    hitbox.UseGravity = !hitbox.UseGravity;
                lastDoubleTapTime = Time.time;
            }

            speed = Mathf.MoveTowards(speed, targetSpeed, speedDelta);

            if (hitbox.UseGravity) {
                velocity.y = hitbox.Velocity.y;
            } else {
                velocity.y = Input.GetAxis("Fly") * speed;
            }

            velocity.x = input.x * speed;
            velocity.z = input.y * speed;
            velocity = Quaternion.Euler(0, camera.localEulerAngles.y, 0) * velocity;

            hitbox.Velocity = velocity;
        }
    }
}