using UnityEngine;

namespace Minecraft.Player {
    public class GroundChecker : MonoBehaviour {
        public bool IsGrounded => Physics.CheckSphere(transform.position + offset, radius, groundLayers);

        [SerializeField]
        private Vector3 offset = Vector3.zero;
        [SerializeField, Min(0)]
        private float radius = 0.5f;
        [SerializeField]
        private LayerMask groundLayers = ~0;

        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + offset, radius);
        }
    }
}