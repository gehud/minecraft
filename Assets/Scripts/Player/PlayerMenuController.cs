using UnityEngine;

namespace Minecraft.Player {
    public class PlayerMenuController : MonoBehaviour {
        [SerializeField]
        private GameObject menu;

        private void Update() {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                if (!menu.activeSelf) {
                    menu.SetActive(true);
                    Cursor.lockState = CursorLockMode.Confined;
                } else {
                    menu.SetActive(false);
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }
    }
}