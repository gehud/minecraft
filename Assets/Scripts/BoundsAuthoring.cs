using UnityEngine;

namespace Minecraft {
	public class BoundsAuthoring : MonoBehaviour {
		public Bounds Value => new(center, size);

		[SerializeField] private Vector3 center;
		[SerializeField] private Vector3 size;

		private void OnDrawGizmosSelected() {
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(transform.position + center, size);
		}
	}
}