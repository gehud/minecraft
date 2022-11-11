using UnityEngine;

namespace Minecraft.Player {
    public class BoxGroundChecker : GroundChecker {
        public override bool IsGrounded => Physics.CheckBox(transform.position + offset, 
                                                            halfExtents, 
                                                            Quaternion.identity,
                                                            layerMask);

        [SerializeField] 
        private Vector3 offset = Vector3.zero;
        [SerializeField]
        private Vector3 halfExtents = Vector3.one / 2;
        [SerializeField]
        private LayerMask layerMask = ~0;

        protected override void Visualize() {
            base.Visualize();
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + offset, halfExtents * 2);
        }
    }
}