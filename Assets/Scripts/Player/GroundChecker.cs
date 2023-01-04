using UnityEngine;

namespace Minecraft.Player {
    public class GroundChecker : MonoBehaviour {
        public bool IsGrounded => Physics.CheckBox(transform.position + offset, 
                                                   halfExtents, 
                                                   Quaternion.identity,
                                                   layerMask);

        [SerializeField] 
        private Vector3 offset = Vector3.zero;
        [SerializeField]
        private Vector3 halfExtents = Vector3.one / 2;
        [SerializeField]
        private LayerMask layerMask = ~0;

        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + offset, halfExtents);
        }
    }
}