using TMPro;
using UnityEngine;

namespace Minecraft.UI {
	[RequireComponent(typeof(TMP_Text))]
	public class SelectedBlockText : MonoBehaviour {
		public TMP_Text Text => text;

		private TMP_Text text;

		private void Awake() {
			text = GetComponent<TMP_Text>();
		}
	}
}