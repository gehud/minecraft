using UnityEngine;

namespace Minecraft.Player {
    public abstract class GroundChecker : MonoBehaviour, IGroundChecker {
        public abstract bool IsGrounded { get; }

        protected virtual void Visualize() { }

        private void OnDrawGizmosSelected() {
            Visualize();
        }
    }
}