using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Minecraft.UI {
	public class HoverTextColorSwitcher : UIBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler {
		[SerializeField]
		private TMP_Text text;
		[SerializeField]
		private Color normal = Color.black;
		[SerializeField]
		private Color hover = Color.white;

		private bool isSelected = false;

		private new void Start() {
			text.color = normal;
		}

		void ISelectHandler.OnSelect(BaseEventData eventData) {
			text.color = hover;
			isSelected = true;
		}

		void IDeselectHandler.OnDeselect(BaseEventData eventData) {
			text.color = normal;
			isSelected = false;
		}

		void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
			text.color = hover;
		}

		void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
			if (!isSelected) {
				text.color = normal;
			}
		}
	}
}
