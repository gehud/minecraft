using Minecraft.Extensions;
using Minecraft.Physics;
using Minecraft.UI;
using Minecraft.Utilities;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Minecraft.Player {
	[RequireComponent(typeof(Hitbox))]
    public class MovementController : NetworkBehaviour {
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

		[Inject]
		private readonly World world;

		[Inject]
		private readonly BlockProvider blockProvider;

		[Inject]
		private readonly PhysicsWorld PhysicsWorld;

        [Inject]
        private readonly UIController uIController;

        [Inject]
        private readonly ISavePayload savePayload;

		private float lastDoubleTapTime = 0.0f;

        private Hitbox hitbox;
        private Vector3 velocity = Vector3.zero;
        private float targetSpeed = 0;
        private float speed = 0;
        private bool isGrounded = false;
        private bool isSneaking = false;
		private bool isSprinting = false;
		private bool isSwiming = false;
		private Vector3 lastPosition;

        private IInputProvider inputProvider;

		private void OnEnable() {
            inputProvider = GetComponent<IInputProvider>();
			hitbox = GetComponent<Hitbox>();
            hitbox.enabled = true;

            for (int y = World.HEIGHT * Chunk.SIZE; y >= 0; --y) {
                if (blockProvider.Get(world.GetBlock(new Vector3Int(0, y, 0))).IsSolid) {
                    transform.position = new Vector3(0.0f, y + 1.0f, 0.0f);
                    break;
                }
            }

            lastPosition = transform.position;
		}

		private void Jump() {
            float velocity = Mathf.Sqrt(2 * Mathf.Abs(PhysicsWorld.Gravity.y) * jumpingHeight);
            hitbox.Velocity += Vector3.up * velocity;
        }

        private void Update() {
			if (savePayload.Role != ConnectionRoles.None && !IsOwner)
				return;

			if (uIController.IsUsing)
                return;

            if (isSneaking) {
				var extents = hitbox.Bounds.extents;
				var offset = hitbox.Bounds.center;
				int y = Mathf.FloorToInt(transform.position.y + offset.y - extents.y - skinWidth);

                bool shouldFall = true;
				for (int x = Mathf.FloorToInt(lastPosition.x + offset.x - extents.x + skinWidth); x <= Mathf.FloorToInt(lastPosition.x + offset.x + extents.x - skinWidth); x++) {
					for (int z = Mathf.FloorToInt(transform.position.z + offset.z - extents.z + skinWidth); z <= Mathf.FloorToInt(transform.position.z + offset.z + extents.z - skinWidth); z++) {
						if (blockProvider.Get(world.GetBlock(x, y, z)).IsSolid) {
                            shouldFall = false;
                            break;
						}
					}
				}

                if (shouldFall)
                    transform.position = new Vector3(transform.position.x, transform.position.y, lastPosition.z);

                shouldFall = true;
				for (int x = Mathf.FloorToInt(transform.position.x + offset.x - extents.x + skinWidth); x <= Mathf.FloorToInt(transform.position.x + offset.x + extents.x - skinWidth); x++) {
					for (int z = Mathf.FloorToInt(lastPosition.z + offset.z - extents.z + skinWidth); z <= Mathf.FloorToInt(lastPosition.z + offset.z + extents.z - skinWidth); z++) {
						if (blockProvider.Get(world.GetBlock(x, y, z)).IsSolid) {
							shouldFall = false;
							break;
						}
					}
				}

				if (shouldFall)
					transform.position = new Vector3(lastPosition.x, transform.position.y, transform.position.z);
			}

            isGrounded = IsGrounded();

            if (isGrounded && hitbox.IsKinematic)
                hitbox.IsKinematic = false;

            Vector2 input = inputProvider.Movement.NormalizeSmooth();

            isSneaking = Input.GetKey(sneakKey) && isGrounded;
            isSprinting = Input.GetKeyDown(sprintKey) || isSprinting && input.magnitude == 1.0f;
            isSwiming = blockProvider.Get(world.GetBlock(CoordinateUtility.ToCoordinate(transform.position))).IsLiquid;

            if (isSneaking) {
                targetSpeed = sneakSpeed;
            } else if (isSprinting) {
                targetSpeed = sprintSpeed;
            } else {
                targetSpeed = walkSpeed;
            }

            if (Input.GetKeyDown(jumpKey)) {
                if (isGrounded && !isSwiming)
                    Jump();
                if (Time.time - lastDoubleTapTime < doubleTapTime)
                    hitbox.IsKinematic = !hitbox.IsKinematic;
                lastDoubleTapTime = Time.time;
            }

            speed = Mathf.MoveTowards(speed, targetSpeed, speedDelta);

            if (!hitbox.IsKinematic) {
                if (isSwiming && Input.GetAxis("Fly") > 0) {
					velocity.y = Input.GetAxis("Fly") * speed;
				} else { 
                    velocity.y = hitbox.Velocity.y;
                }
            } else {
                velocity.y = Input.GetAxis("Fly") * speed;
            }

            velocity.x = input.x * speed;
            velocity.z = input.y * speed;
            velocity = Quaternion.Euler(0, camera.localEulerAngles.y, 0) * velocity;

            hitbox.Velocity = velocity;
            lastPosition = transform.position;
        }

		private bool IsGrounded() {
			var extents = hitbox.Bounds.extents;
			var offset = hitbox.Bounds.center;
			int y = Mathf.FloorToInt(transform.position.y + offset.y - extents.y - skinWidth);
			for (int x = Mathf.FloorToInt(transform.position.x + offset.x - extents.x + skinWidth); x <= Mathf.FloorToInt(transform.position.x + offset.x + extents.x - skinWidth); x++) {
                for (int z = Mathf.FloorToInt(transform.position.z + offset.z - extents.z + skinWidth); z <= Mathf.FloorToInt(transform.position.z + offset.z + extents.z - skinWidth); z++) {
                    if (blockProvider.Get(world.GetBlock(x, y, z)).IsSolid) {
                        return true;
                    }
                }
            }

            return false;
		}
    }
}