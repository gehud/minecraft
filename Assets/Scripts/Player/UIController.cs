using UnityEngine;
using Minecraft.UI;

namespace Minecraft.Player {
	public class UIController : MonoBehaviour {
		[SerializeField]
		private DebugDisplay debugDisplay;
		[SerializeField]
		private GameObject ui;

		private void Update() {
			if (Input.GetKeyDown(KeyCode.F1)) {
				ui.SetActive(!ui.activeSelf);
			}

			if (Input.GetKeyDown(KeyCode.F3)) {
				debugDisplay.gameObject.SetActive(!debugDisplay.gameObject.activeSelf);
			}
		}
	}
}