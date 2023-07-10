using Minecraft.UI;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Minecraft {
	public class LoadingMessage : MonoBehaviour {
		[SerializeField]
		private Image bar;
		[SerializeField]
		private LoadingText text;

		[Inject]
		private readonly ChunkLoader chunkLoader;

		private IEnumerator Start() {
			bar.fillAmount = 0.0f;
			text.Play();

			while (bar.fillAmount != 1.0f) {
				bar.fillAmount = chunkLoader.WorldLoadingProgress;
				yield return null;
			}

			yield return new WaitForSeconds(1.0f);	

			gameObject.SetActive(false);
		}
	}
}
