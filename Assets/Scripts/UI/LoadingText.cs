using System.Collections;
using TMPro;
using UnityEngine;

namespace Minecraft.UI {
	[RequireComponent(typeof(TMP_Text))]
	public class LoadingText : MonoBehaviour {
		public void Play() {
			StartCoroutine(PlayAnimation());
		}

		private IEnumerator PlayAnimation() {
			var text = GetComponent<TMP_Text>();
			var waitForOneSecond = new WaitForSeconds(1.0f);
			int numDots = 0;
			while (true) {
				text.text = "Loading..."[..("Loading".Length + numDots)];
				++numDots;
				if (numDots > 3)
					numDots = 0;
				yield return waitForOneSecond;
			}
		}
	}
}
