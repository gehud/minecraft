using System;
using UnityEngine;

namespace Minecraft.Player {
    public class MenuController : MonoBehaviour {
        public static event Action OnEnter;
        public static event Action OnExit;

		[SerializeField]
        private GameObject menu;

        private void Update() {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                if (!menu.activeSelf) {
                    menu.SetActive(true);
                    Cursor.lockState = CursorLockMode.Confined;
                    OnEnter?.Invoke();
                } else {
                    menu.SetActive(false);
                    Cursor.lockState = CursorLockMode.Locked;
                    OnExit?.Invoke();   
                }
            }
        }
    }
}