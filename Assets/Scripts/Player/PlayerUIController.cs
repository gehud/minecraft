using UnityEngine;
using Minecraft.UI;

namespace Minecraft.Player {
	public class PlayerUIController : MonoBehaviour {
		[SerializeField]
		private DebugDisplay debugDisplay;

		private void Update() {
			if (Input.GetKeyDown(KeyCode.F3)) {
				debugDisplay.gameObject.SetActive(!debugDisplay.gameObject.activeSelf);
			}
		}
	}
}