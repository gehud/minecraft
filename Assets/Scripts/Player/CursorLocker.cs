using UnityEngine;

namespace Minecraft.Player {
    public class CursorLocker : MonoBehaviour {

        private void Start() {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}