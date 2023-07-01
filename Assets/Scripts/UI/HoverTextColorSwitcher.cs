using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Minecraft.UI {
	public class HoverTextColorSwitcher : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
		[SerializeField]
		private TMP_Text text;
		[SerializeField]
		private Color normal = Color.black;
		[SerializeField]
		private Color hover = Color.white;

		private void Start() {
			text.color = normal;
		}

		void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
			text.color = hover;
		}

		void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
			text.color = normal;
		}

		void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
			text.color = normal;
		}
	}
}
