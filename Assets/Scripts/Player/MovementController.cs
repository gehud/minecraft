using Minecraft.Physics;
using Minecraft.Utilities;
using System;
using TMPro;
using UnityEngine;
using Zenject;

namespace Minecraft.Player {
	[RequireComponent(typeof(Hitbox))]
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
        [SerializeField, Min(0)]
        private float skinWidth = 0.08f;

        [Header("Keys")]
        [SerializeField]
        private KeyCode jumpKey = KeyCode.Space;
        [SerializeField]
        private KeyCode sneakKey = KeyCode.LeftShift;
        [SerializeField]
        private KeyCode sprintKey = KeyCode.LeftControl;

        private float lastDoubleTapTime = 0.0f;

        private Hitbox hitbox;
        private Vector3 velocity = Vector3.zero;
        private float targetSpeed = 0;
        private float speed = 0;
        private bool isSneaking = false;
        private bool isSprinting = false;

        [Inject]
        private readonly World World;

        [Inject]
        private readonly BlockDataProvider BlockDataProvider;

        [Inject]
        private readonly PhysicsWorld PhysicsWorld;

        private void Awake() {
			hitbox = GetComponent<Hitbox>();

			for (int y = World.HEIGHT * Chunk.SIZE; y >= 0; --y) {
                if (BlockDataProvider.Get(World.GetBlock(new Vector3Int(0, y, 0))).IsSolid) {
                    transform.position = new Vector3(0.0f, y + 1.0f, 0.0f);
                    break;
                }
            }
        }

		private void Jump() {
            float velocity = Mathf.Sqrt(2 * Mathf.Abs(PhysicsWorld.Gravity.y) * jumpingHeight);
            hitbox.Velocity += Vector3.up * velocity;
        }

        private void Update() {
            bool isGrounded = IsGrounded();

            if (isGrounded) {
                if (hitbox.IsKinematic)
                    hitbox.IsKinematic = false;
            }

            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            Vector2 input = new(horizontalInput, verticalInput);
            input = input.magnitude > 1 ? input.normalized : input;

            isSneaking = Input.GetKey(sneakKey);
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
                    hitbox.IsKinematic = !hitbox.IsKinematic;
                lastDoubleTapTime = Time.time;
            }

            speed = Mathf.MoveTowards(speed, targetSpeed, speedDelta);

            if (!hitbox.IsKinematic) {
                velocity.y = hitbox.Velocity.y;
            } else {
                velocity.y = Input.GetAxis("Fly") * speed;
            }

            velocity.x = input.x * speed;
            velocity.z = input.y * speed;
            velocity = Quaternion.Euler(0, camera.localEulerAngles.y, 0) * velocity;

            hitbox.Velocity = velocity;
        }

		private bool IsGrounded() {
			var extents = hitbox.Bounds.extents;
			var offset = hitbox.Bounds.center;
			int y = Mathf.FloorToInt(transform.position.y + offset.y - extents.y - skinWidth);
			for (int x = Mathf.FloorToInt(transform.position.x + offset.x - extents.x + skinWidth); x <= Mathf.FloorToInt(transform.position.x + offset.x + extents.x - skinWidth); x++) {
                for (int z = Mathf.FloorToInt(transform.position.z + offset.z - extents.z + skinWidth); z <= Mathf.FloorToInt(transform.position.z + offset.z + extents.z - skinWidth); z++) {
                    if (BlockDataProvider.Get(World.GetBlock(x, y, z)).IsSolid) {
                        return true;
                    }
                }
            }

            return false;
		}
    }
}