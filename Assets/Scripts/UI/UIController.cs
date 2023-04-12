using System.Collections.Generic;
using UnityEngine;

namespace Minecraft.UI {
	public class UIController : MonoBehaviour {
		public bool IsUsing => isUsing;
		private bool isUsing = false;

		[SerializeField]
		private DebugDisplay debugDisplay;
		[SerializeField]
		private GameObject ui;
		[SerializeField]
		private GameObject menu;
		[SerializeField]
		private Console console;

		private readonly List<GameObject> usingObjects = new();

		private void Update() {
			if (Input.GetKeyDown(KeyCode.F1)) {
				ui.SetActive(!ui.activeSelf);
			}

			if (Input.GetKeyDown(KeyCode.F3)) {
				debugDisplay.gameObject.SetActive(!debugDisplay.gameObject.activeSelf);
			}

			if (Input.GetKeyDown(KeyCode.Escape)) {
				menu.SetActive(!menu.activeSelf);
				if (menu.gameObject.activeSelf) {
					usingObjects.Add(menu.gameObject);
				} else {
					usingObjects.Remove(menu.gameObject);
				}
			}

			if (Input.GetKeyDown(KeyCode.BackQuote)) {
				console.gameObject.SetActive(!console.gameObject.activeSelf);
				if (console.gameObject.activeSelf) {
					usingObjects.Add(console.gameObject);
				} else {
					usingObjects.Remove(console.gameObject);
				}
			}

			if (usingObjects.Count == 0) {
				isUsing = false;
				Cursor.lockState = CursorLockMode.Locked;
			} else {
				isUsing = true;
				Cursor.lockState = CursorLockMode.Confined;
			}
		}
	}
}