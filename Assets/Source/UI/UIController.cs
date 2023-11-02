using UnityEngine;
using UnityEngine.InputSystem;

namespace Minecraft.UI {
    public class UIController : MonoBehaviour {
        [SerializeField]
        private GameObject inventory;

        private Controls controls;

        private void OnInventory(InputAction.CallbackContext context) {
            inventory.SetActive(!inventory.activeSelf);
        }

        private void Awake() {
            controls = new();
            controls.UI.Inventory.performed += OnInventory;
        }

        private void OnEnable() {
            controls.Enable();
        }

        private void OnDisable() {
            controls.Disable();
        }
    }
}