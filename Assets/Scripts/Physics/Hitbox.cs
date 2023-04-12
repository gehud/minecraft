using System;
using Unity.Netcode;
using UnityEngine;

namespace Minecraft.Physics {
	public class Hitbox : MonoBehaviour {
		public Bounds Bounds => new(center, size);

		public Vector3 Velocity { get; set; }

		public bool IsKinematic {
			get => isKinematic;
			set => isKinematic = value;
		}

		internal static event Action<Hitbox> OnAdd;
		internal static event Action<Hitbox> OnRemove;

		[SerializeField] private Vector3 center;
		[SerializeField] private Vector3 size = Vector3.one;
		[SerializeField] private bool isKinematic = false;

		private void OnEnable() {
			OnAdd?.Invoke(this);
		}

		private void OnDisable() {
			OnRemove?.Invoke(this);
		}

		private void OnDrawGizmosSelected() {
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(transform.position + center, size);
		}
	}
}