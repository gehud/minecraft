using System.Linq;
using UnityEngine;

namespace Minecraft.UI {
    public class CursorSwitcher : MonoBehaviour {
        [SerializeField]
        private GameObject background;
        [SerializeField]
        private GameObject[] uI;

        private void Update() {
            if (uI.Any(@object => @object.activeSelf)) {
                Cursor.lockState = CursorLockMode.None;
                background.SetActive(true);
            } else {
                Cursor.lockState = CursorLockMode.Locked;
                background.SetActive(false);
            }
        }
    }
}