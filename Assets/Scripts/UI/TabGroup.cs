using System.Linq;
using UnityEngine;

namespace Minecraft.UI {
	public class TabGroup : MonoBehaviour {
		[SerializeField]
		private TabButton[] tabButtons;

		private void SetActiveTab(TabButton tabButton) {
			if (!tabButtons.Contains(tabButton))
				return;

			foreach (var tab in tabButtons) {
				if (tab == tabButton)
					tab.Content.SetActive(true);
				else
					tab.Content.SetActive(false);
			}
		}

		private void OnEnable() {
			TabButton.OnClicked += SetActiveTab;
		}

		private void OnDisable() {
			TabButton.OnClicked -= SetActiveTab;
		}
	}
}