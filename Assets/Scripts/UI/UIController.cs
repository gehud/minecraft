using UnityEngine;

namespace Minecraft.UI
{
    public class UIController : MonoBehaviour
    {
        [SerializeField]
        private GameObject inventory;
        [SerializeField]
        private GameObject debugMonitor;

        private Controls controls;

        private void Toggle(GameObject gameObject)
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        private void Awake()
        {
            controls = new();
            controls.UI.Inventory.performed += (_) => Toggle(inventory);
            controls.UI.DebugMonitor.performed += (_) => Toggle(debugMonitor);
        }

        private void OnEnable()
        {
            controls.Enable();
        }

        private void OnDisable()
        {
            controls.Disable();
        }
    }
}