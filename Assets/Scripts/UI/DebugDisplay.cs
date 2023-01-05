using Minecraft.Utilities;
using System.Collections;
using TMPro;
using UnityEngine;
using Zenject;

namespace Minecraft.UI {
	public class DebugDisplay : MonoBehaviour {
		[SerializeField]
		private Transform player;
		[SerializeField]
		private new Camera camera;
		[SerializeField]
		private float framerateUpdate = 0.5f;

		[SerializeField]
		private TMP_Text framerateText;
		[SerializeField]
		private TMP_Text positionText;
		[SerializeField]
		private TMP_Text continentalnessText;
		[SerializeField]
		private TMP_Text peaksAndValleysText;
		[SerializeField]
		private TMP_Text erosionText;

		[Inject]
		private ChunkDataGenerator ChunkDataGenerator { get; }

		private int framerate;

		private void Start() {
			gameObject.SetActive(false);
		}

		private void Update() {
			framerateText.text = framerate.ToString();
			positionText.text = player.position.ToString();
			var playerCoordinate = CoordinateUtility.ToCoordinate(player.position);
			continentalnessText.text = ChunkDataGenerator.GetContinentalness(playerCoordinate).ToString("F2");
			peaksAndValleysText.text = ChunkDataGenerator.GetPeaksAndValleys(playerCoordinate).ToString("F2");
			erosionText.text = ChunkDataGenerator.GetErosion(playerCoordinate).ToString("F2");
		}

		private void OnEnable() {
			StartCoroutine(nameof(UpdateFramerate));
		}

		private void OnDisable() {
			StopCoroutine(nameof(UpdateFramerate));
		}

		private IEnumerator UpdateFramerate() {
			while (true) {
				framerate = (int)(1.0f / Time.smoothDeltaTime);
				yield return new WaitForSeconds(framerateUpdate);
			}
		}
	}
}