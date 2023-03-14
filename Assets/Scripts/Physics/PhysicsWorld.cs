using Minecraft.Utilities;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Minecraft.Physics {

	public class PhysicsWorld : MonoBehaviour {
		public Vector3 Gravity {
			get => gravity;
			set => gravity = value;
		}

		public float ContactOffset {
			get => contactOffset;
			set => contactOffset = value;
		}

		[SerializeField] private Vector3 gravity = new(0.0f, -9.81f, 0.0f);
		[SerializeField] private float contactOffset = 0.08f;
		[SerializeField, Range(0.0f, 1.0f)] private float waterFriction = 0.1f;

		[Inject]
		private readonly World world;

		[Inject]
		private readonly BlockProvider blockProvider;

		private readonly List<Hitbox> hitboxes = new();

		public bool Raycast(Ray ray, float maxDistance, out RaycastHit raycastHit) {
			float px = ray.origin.x;
			float py = ray.origin.y;
			float pz = ray.origin.z;

			float dx = ray.direction.x;
			float dy = ray.direction.y;
			float dz = ray.direction.z;

			float t = 0.0f;
			float ix = Mathf.Floor(px);
			float iy = Mathf.Floor(py);
			float iz = Mathf.Floor(pz);

			float stepx = dx > 0.0f ? 1.0f : -1.0f;
			float stepy = dy > 0.0f ? 1.0f : -1.0f;
			float stepz = dz > 0.0f ? 1.0f : -1.0f;

			float infinity = float.PositiveInfinity;

			float txDelta = dx == 0.0f ? infinity : Mathf.Abs(1.0f / dx);
			float tyDelta = dy == 0.0f ? infinity : Mathf.Abs(1.0f / dy);
			float tzDelta = dz == 0.0f ? infinity : Mathf.Abs(1.0f / dz);

			float xdist = stepx > 0 ? ix + 1 - px : px - ix;
			float ydist = stepy > 0 ? iy + 1 - py : py - iy;
			float zdist = stepz > 0 ? iz + 1 - pz : pz - iz;

			float txMax = txDelta < infinity ? txDelta * xdist : infinity;
			float tyMax = tyDelta < infinity ? tyDelta * ydist : infinity;
			float tzMax = tzDelta < infinity ? tzDelta * zdist : infinity;

			int steppedIndex = -1;

			Vector3 end;
			Vector3 iend;
			Vector3 norm;

			while (t <= maxDistance) {
				var block = world.GetBlock((int)ix, (int)iy, (int)iz);
				if (blockProvider.Get(block).IsSolid) {
					end.x = px + t * dx;
					end.y = py + t * dy;
					end.z = pz + t * dz;

					iend.x = ix;
					iend.y = iy;
					iend.z = iz;

					norm.x = norm.y = norm.z = 0.0f;
					if (steppedIndex == 0) norm.x = -stepx;
					if (steppedIndex == 1) norm.y = -stepy;
					if (steppedIndex == 2) norm.z = -stepz;

					raycastHit = new() {
						point = iend,
						normal = norm
					};

					return true;
				}
				if (txMax < tyMax) {
					if (txMax < tzMax) {
						ix += stepx;
						t = txMax;
						txMax += txDelta;
						steppedIndex = 0;
					} else {
						iz += stepz;
						t = tzMax;
						tzMax += tzDelta;
						steppedIndex = 2;
					}
				} else {
					if (tyMax < tzMax) {
						iy += stepy;
						t = tyMax;
						tyMax += tyDelta;
						steppedIndex = 1;
					} else {
						iz += stepz;
						t = tzMax;
						tzMax += tzDelta;
						steppedIndex = 2;
					}
				}
			}
			iend.x = ix;
			iend.y = iy;
			iend.z = iz;

			end.x = px + t * dx;
			end.y = py + t * dy;
			end.z = pz + t * dz;
			norm.x = norm.y = norm.z = 0.0f;

			raycastHit = new() {
				point = iend,
				normal = norm
			};

			return false;
		}

		private void Step(float delta) {
			for (int i = 0; i < hitboxes.Count; i++) {
				var hitbox = hitboxes[i];
				var transform = hitbox.transform;

				if (hitbox == null)
					return;

				var extents = hitbox.Bounds.extents;
				var offset = hitbox.Bounds.center;
				if (!hitbox.IsKinematic)
					hitbox.Velocity += gravity * delta;

				if (hitbox.Velocity.x < 0.0f) {
					int x = Mathf.FloorToInt(transform.position.x + offset.x - extents.x - contactOffset);
					for (int y = Mathf.FloorToInt(transform.position.y + offset.y - extents.y + contactOffset); y <= Mathf.FloorToInt(transform.position.y + offset.y + extents.y - contactOffset); y++) {
						for (int z = Mathf.FloorToInt(transform.position.z + offset.z - extents.z + contactOffset); z <= Mathf.FloorToInt(transform.position.z + offset.z + extents.z - contactOffset); z++) {
							if (blockProvider.Get(world.GetBlock(new Vector3Int(x, y, z))).IsSolid) {
								hitbox.Velocity = new Vector3(0.0f, hitbox.Velocity.y, hitbox.Velocity.z);
								transform.position = new Vector3(x + 1.0f - offset.x + extents.x + contactOffset, transform.position.y, transform.position.z);
								break;
							}
						}
					}
				}

				if (hitbox.Velocity.x > 0.0f) {
					int x = Mathf.FloorToInt(transform.position.x + offset.x + extents.x + contactOffset);
					for (int y = Mathf.FloorToInt(transform.position.y + offset.y - extents.y + contactOffset); y <= Mathf.FloorToInt(transform.position.y + offset.y + extents.y - contactOffset); y++) {
						for (int z = Mathf.FloorToInt(transform.position.z + offset.z - extents.z + contactOffset); z <= Mathf.FloorToInt(transform.position.z + offset.z + extents.z - contactOffset); z++) {
							if (blockProvider.Get(world.GetBlock(new Vector3Int(x, y, z))).IsSolid) {
								hitbox.Velocity = new Vector3(0.0f, hitbox.Velocity.y, hitbox.Velocity.z);
								transform.position = new Vector3(x - offset.x - extents.x - contactOffset, transform.position.y, transform.position.z);
								break;
							}
						}
					}
				}

				if (hitbox.Velocity.z < 0.0f) {
					int z = Mathf.FloorToInt(transform.position.z + offset.z - extents.z - contactOffset);
					for (int y = Mathf.FloorToInt(transform.position.y + offset.y - extents.y + contactOffset); y <= Mathf.FloorToInt(transform.position.y + offset.y + extents.y - contactOffset); y++) {
						for (int x = Mathf.FloorToInt(transform.position.x + offset.x - extents.x + contactOffset); x <= Mathf.FloorToInt(transform.position.x + offset.x + extents.x - contactOffset); x++) {
							if (blockProvider.Get(world.GetBlock(new Vector3Int(x, y, z))).IsSolid) {
								hitbox.Velocity = new Vector3(hitbox.Velocity.x, hitbox.Velocity.y, 0.0f);
								transform.position = new Vector3(transform.position.x, transform.position.y, z + 1.0f - offset.z + extents.z + contactOffset);
								break;
							}
						}
					}
				}

				if (hitbox.Velocity.z > 0.0f) {
					int z = Mathf.FloorToInt(transform.position.z + offset.z + extents.z + contactOffset);
					for (int y = Mathf.FloorToInt(transform.position.y + offset.y - extents.y + contactOffset); y <= Mathf.FloorToInt(transform.position.y + offset.y + extents.y - contactOffset); y++) {
						for (int x = Mathf.FloorToInt(transform.position.x + offset.x - extents.x + contactOffset); x <= Mathf.FloorToInt(transform.position.x + offset.x + extents.x - contactOffset); x++) {
							if (blockProvider.Get(world.GetBlock(new Vector3Int(x, y, z))).IsSolid) {
								hitbox.Velocity = new Vector3(hitbox.Velocity.x, hitbox.Velocity.y, 0.0f);
								transform.position = new Vector3(transform.position.x, transform.position.y, z - offset.z - extents.z - contactOffset);
								break;
							}
						}
					}
				}

				if (hitbox.Velocity.y < 0.0f) {
					int y = Mathf.FloorToInt(transform.position.y + offset.y - extents.y - contactOffset);
					for (int x = Mathf.FloorToInt(transform.position.x + offset.x - extents.x + contactOffset); x <= Mathf.FloorToInt(transform.position.x + offset.x + extents.x - contactOffset); x++) {
						for (int z = Mathf.FloorToInt(transform.position.z + offset.z - extents.z + contactOffset); z <= Mathf.FloorToInt(transform.position.z + offset.z + extents.z - contactOffset); z++) {
							if (blockProvider.Get(world.GetBlock(new Vector3Int(x, y, z))).IsSolid) {
								hitbox.Velocity = new Vector3(hitbox.Velocity.x, 0.0f, hitbox.Velocity.z);
								transform.position = new Vector3(transform.position.x, y + 1.0f - offset.y + extents.y + contactOffset, transform.position.z);
								break;
							}
						}
					}
				}

				if (hitbox.Velocity.y > 0.0f) {
					int y = Mathf.FloorToInt(transform.position.y + offset.y + extents.y + contactOffset);
					for (int x = Mathf.FloorToInt(transform.position.x + offset.x - extents.x + contactOffset); x <= Mathf.FloorToInt(transform.position.x + offset.x + extents.x - contactOffset); x++) {
						for (int z = Mathf.FloorToInt(transform.position.z + offset.z - extents.z + contactOffset); z <= Mathf.FloorToInt(transform.position.z + offset.z + extents.z - contactOffset); z++) {
							if (blockProvider.Get(world.GetBlock(new Vector3Int(x, y, z))).IsSolid) {
								hitbox.Velocity = new Vector3(hitbox.Velocity.x, 0.0f, hitbox.Velocity.z);
								transform.position = new Vector3(transform.position.x, y - offset.y - extents.y - contactOffset, transform.position.z);
								break;
							}
						}
					}
				}

				var blockCoordinate = CoordinateUtility.ToCoordinate(transform.position);
				if (!hitbox.IsKinematic && blockProvider.Get(world.GetBlock(blockCoordinate)).IsLiquid)
					hitbox.Velocity *= 1.0f - waterFriction;

				transform.position += hitbox.Velocity * delta;
			}
		}

		private void AddHitbox(Hitbox hitbox) {
			hitboxes.Add(hitbox);
		}

		private void RemoveHitbox(Hitbox hitbox) {
			hitboxes.Remove(hitbox);
		}

		private void OnEnable() {
			Hitbox.OnAdd += AddHitbox;
			Hitbox.OnRemove += RemoveHitbox;
		}

		private void OnDisable() {
			Hitbox.OnAdd -= AddHitbox;
			Hitbox.OnRemove -= RemoveHitbox;
		}

		private void FixedUpdate() {
			int steps = 20;
			for (int i = 0; i < steps; i++) {
				Step(Time.fixedDeltaTime / steps);
			}
		}
	}
}