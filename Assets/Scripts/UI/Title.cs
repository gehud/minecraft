using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Minecraft.UI {
	public class Title : MonoBehaviour {
		[SerializeField]
		private Image front;
		[SerializeField]
		private Image back;
		[SerializeField]
		private float time = 5.0f;
		[SerializeField]
		private float speed = 2.0f;
		[SerializeField]
		private Sprite[] titles;

		private int index = 0;

		private void MoveTowards() {
			++index;
			if (index == titles.Length)
				index = 0;
		}

		private void Start() {
			StartCoroutine(UpdateTitles());
		}

		private IEnumerator UpdateTitles() {
			front.sprite = titles[index];
			MoveTowards();
			back.sprite = titles[index];
			MoveTowards();
			while (true) {
				yield return new WaitForSeconds(time);
				while (front.color.a > 0) {
					var color = front.color;
					front.color = new Color(color.r, color.g, color.b, color.a - speed * Time.deltaTime);
					yield return null;
				}
				front.sprite = back.sprite;
				front.color = Color.white;
				back.sprite = titles[index];
				MoveTowards();
			}
		}
	}
}