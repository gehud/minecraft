using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace Minecraft.UI {
	[RequireComponent(typeof(Slider))]
	public class DrawDistanceSlider : MonoBehaviour, IPointerUpHandler {
		[Inject]
		private readonly World world;

		private Slider slider;

		private void Awake() {
			slider = GetComponent<Slider>();
			slider.minValue = World.MIN_DRAW_DISTANCE;
			slider.maxValue = World.MAX_DRAW_DISTANCE;
			slider.wholeNumbers = true;
			slider.value = world.DrawDistance;
		}

		void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
			world.DrawDistance = (int)slider.value;
		}
	}
}