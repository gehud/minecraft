using System;
using UnityEngine;

namespace Minecraft.UI {
	public class SaveView : MonoBehaviour {
		public static event Action<SaveView> OnPlay;
		public static event Action<SaveView> OnDelete;

		public Save Model { get; set; }

		public void Play() {
			OnPlay?.Invoke(this);
		}

		public void Delete() { 
			OnDelete?.Invoke(this);
		}
	}
}