using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Minecraft.UI {
	public class TabButton : MonoBehaviour, IPointerClickHandler {
		public static event Action<TabButton> OnClicked;

		public GameObject Content => content;

		[SerializeField]
		private GameObject content;

		void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
			OnClicked?.Invoke(this);
		}
	}
}