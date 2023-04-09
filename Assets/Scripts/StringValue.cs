using UnityEngine;
using UnityEngine.Events;

namespace Minecraft {
	public class StringValue : MonoBehaviour {
		public string Value => value;

		[SerializeField]
		private UnityEvent<string> onValueChanged;

		private string value;

		public void FromFloat(float value) {
			this.value = value.ToString();
			onValueChanged.Invoke(Value);
		}
	}
}