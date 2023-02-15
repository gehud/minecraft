using UnityEngine;
using Zenject;
using static Zenject.CheatSheet;

namespace Minecraft {
	public class Hitbox {
		public Vector3 Position;
		public Bounds Bounds;
		public Vector3 Velocity;
		public bool UseGravity = true;
	}

	public class PhysicsSolver : MonoBehaviour {
		[SerializeField] private Vector3 gravity = new(0.0f, -9.81f, 0.0f);
		[SerializeField] private float contactOffset = 0.08f;

		[Inject]
		private World World { get; }

		[Inject]
		private BlockDataManager BlockDataManager { get; }

		private Hitbox playerBox;

		public Hitbox CreatePlayer() {
			playerBox = new Hitbox();
			return playerBox;
		}

		private void Step(float delta) {
			if (playerBox == null)
				return;

			var extents = playerBox.Bounds.extents;
			var offset = playerBox.Bounds.center;
			if (playerBox.UseGravity)
				playerBox.Velocity += gravity * delta;

			if (playerBox.Velocity.x < 0.0f) {
				for (int y = Mathf.FloorToInt(playerBox.Position.y + offset.y - extents.y + contactOffset); y <= Mathf.FloorToInt(playerBox.Position.y + offset.y + extents.y - contactOffset); y++) {
					for (int z = Mathf.FloorToInt(playerBox.Position.z + offset.z - extents.z + contactOffset); z <= Mathf.FloorToInt(playerBox.Position.z + offset.z + extents.z - contactOffset); z++) {
						int x = Mathf.FloorToInt(playerBox.Position.x + offset.x - extents.x - contactOffset);
						if (BlockDataManager.Data[World.GetBlock(new Vector3Int(x, y, z))].IsSolid) {
							playerBox.Velocity.x = 0.0f;
							playerBox.Position.x = x + 1.0f - offset.x + extents.x + contactOffset;
							break;
						}
					}
				}
			}

			if (playerBox.Velocity.x > 0.0f) {
				for (int y = Mathf.FloorToInt(playerBox.Position.y + offset.y - extents.y + contactOffset); y <= Mathf.FloorToInt(playerBox.Position.y + offset.y + extents.y - contactOffset); y++) {
					for (int z = Mathf.FloorToInt(playerBox.Position.z + offset.z - extents.z + contactOffset); z <= Mathf.FloorToInt(playerBox.Position.z + offset.z + extents.z - contactOffset); z++) {
						int x = Mathf.FloorToInt(playerBox.Position.x + offset.x + extents.x + contactOffset);
						if (BlockDataManager.Data[World.GetBlock(new Vector3Int(x, y, z))].IsSolid) {
							playerBox.Velocity.x = 0.0f;
							playerBox.Position.x = x - offset.x - extents.x - contactOffset;
							break;
						}
					}
				}
			}

			if (playerBox.Velocity.z < 0.0f) {
				for (int y = Mathf.FloorToInt(playerBox.Position.y + offset.y - extents.y + contactOffset); y <= Mathf.FloorToInt(playerBox.Position.y + offset.y + extents.y - contactOffset); y++) {
					for (int x = Mathf.FloorToInt(playerBox.Position.x + offset.x - extents.x + contactOffset); x <= Mathf.FloorToInt(playerBox.Position.x + offset.x + extents.x - contactOffset); x++) {
						int z = Mathf.FloorToInt(playerBox.Position.z + offset.z - extents.z - contactOffset);
						if (BlockDataManager.Data[World.GetBlock(new Vector3Int(x, y, z))].IsSolid) {
							playerBox.Velocity.z = 0.0f;
							playerBox.Position.z = z + 1.0f - offset.z + extents.z + contactOffset;
							break;
						}
					}
				}
			}

			if (playerBox.Velocity.z > 0.0f) {
				for (int y = Mathf.FloorToInt(playerBox.Position.y + offset.y - extents.y + contactOffset); y <= Mathf.FloorToInt(playerBox.Position.y + offset.y + extents.y - contactOffset); y++) {
					for (int x = Mathf.FloorToInt(playerBox.Position.x + offset.x - extents.x + contactOffset); x <= Mathf.FloorToInt(playerBox.Position.x + offset.x + extents.x - contactOffset); x++) {
						int z = Mathf.FloorToInt(playerBox.Position.z + offset.z + extents.z + contactOffset);
						if (BlockDataManager.Data[World.GetBlock(new Vector3Int(x, y, z))].IsSolid) {
							playerBox.Velocity.z = 0.0f;
							playerBox.Position.z = z - offset.z - extents.z - contactOffset;
							break;
						}
					}
				}
			}

			if (playerBox.Velocity.y < 0.0f) {
				for (int x = Mathf.FloorToInt(playerBox.Position.x + offset.x - extents.x + contactOffset); x <= Mathf.FloorToInt(playerBox.Position.x + offset.x + extents.x - contactOffset); x++) {
					for (int z = Mathf.FloorToInt(playerBox.Position.z + offset.z - extents.z + contactOffset); z <= Mathf.FloorToInt(playerBox.Position.z + offset.z + extents.z - contactOffset); z++) {
						int y = Mathf.FloorToInt(playerBox.Position.y + offset.y - extents.y - contactOffset);
						if (BlockDataManager.Data[World.GetBlock(new Vector3Int(x, y, z))].IsSolid) {
							playerBox.Velocity.y = 0.0f;
							playerBox.Position.y = y + 1.0f - offset.y + extents.y + contactOffset;
							break;
						}
					}
				}
			}

			if (playerBox.Velocity.y > 0.0f) {
				for (int x = Mathf.FloorToInt(playerBox.Position.x + offset.x - extents.x + contactOffset); x <= Mathf.FloorToInt(playerBox.Position.x + offset.x + extents.x - contactOffset); x++) {
					for (int z = Mathf.FloorToInt(playerBox.Position.z + offset.z - extents.z + contactOffset); z <= Mathf.FloorToInt(playerBox.Position.z + offset.z + extents.z - contactOffset); z++) {
						int y = Mathf.FloorToInt(playerBox.Position.y + offset.y + extents.y + contactOffset);
						if (BlockDataManager.Data[World.GetBlock(new Vector3Int(x, y, z))].IsSolid) {
							playerBox.Velocity.y = 0.0f;
							playerBox.Position.y = y - offset.y - extents.y - contactOffset;
							break;
						}
					}
				}
			}

			playerBox.Position.x += playerBox.Velocity.x * delta;
			playerBox.Position.y += playerBox.Velocity.y * delta;
			playerBox.Position.z += playerBox.Velocity.z * delta;
		}

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

			float stepx = (dx > 0.0f) ? 1.0f : -1.0f;
			float stepy = (dy > 0.0f) ? 1.0f : -1.0f;
			float stepz = (dz > 0.0f) ? 1.0f : -1.0f;

			float infinity = float.PositiveInfinity;

			float txDelta = (dx == 0.0f) ? infinity : Mathf.Abs(1.0f / dx);
			float tyDelta = (dy == 0.0f) ? infinity : Mathf.Abs(1.0f / dy);
			float tzDelta = (dz == 0.0f) ? infinity : Mathf.Abs(1.0f / dz);

			float xdist = (stepx > 0) ? (ix + 1 - px) : (px - ix);
			float ydist = (stepy > 0) ? (iy + 1 - py) : (py - iy);
			float zdist = (stepz > 0) ? (iz + 1 - pz) : (pz - iz);

			float txMax = (txDelta < infinity) ? txDelta * xdist : infinity;
			float tyMax = (tyDelta < infinity) ? tyDelta * ydist : infinity;
			float tzMax = (tzDelta < infinity) ? tzDelta * zdist : infinity;

			int steppedIndex = -1;

			Vector3 end;
			Vector3 iend;
			Vector3 norm;

			while (t <= maxDistance) {
				BlockType block = World.GetBlock((int)ix, (int)iy, (int)iz);
				if (BlockDataManager.Data[block].IsSolid) {
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

		private void FixedUpdate() {
			int steps = 20;
			for (int i = 0; i < steps; i++) {
				Step(Time.fixedDeltaTime / steps);
			}
		}
	}
}