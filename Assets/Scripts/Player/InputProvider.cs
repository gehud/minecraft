using System;
using UnityEngine;

namespace Minecraft.Player {
	public interface IInputProvider {
		Vector2 Movement { get; }

		Vector2 Look { get; }

		event Action OnLeftMouseButtonDown;

		event Action OnLeftMouseButton;

		event Action OnLeftMouseButtonUp;

		event Action OnRightMouseButtonDown;

		event Action OnRightMouseButton;

		event Action OnRightMouseButtonUp;
	}

	public class InputProvider : MonoBehaviour, IInputProvider {
		public Vector2 Movement => movement;

		public Vector2 Look => look;

		public event Action OnLeftMouseButtonDown;

		public event Action OnLeftMouseButton;

		public event Action OnLeftMouseButtonUp;

		public event Action OnRightMouseButtonDown;

		public event Action OnRightMouseButton;
		
		public event Action OnRightMouseButtonUp;

		private Vector2 movement;
		private Vector2 look;

		private void Update() {
			float horizontal = Input.GetAxis("Horizontal");
			float vertical = Input.GetAxis("Vertical");
			movement = new Vector2(horizontal, vertical);

			float mouseX = Input.GetAxis("Mouse X");
			float mouseY = Input.GetAxis("Mouse Y");
			look = new Vector2(mouseX, mouseY);

			if (Input.GetMouseButtonDown(0)) {
				OnLeftMouseButtonDown?.Invoke();
			} else if (Input.GetMouseButton(0)) {
				OnLeftMouseButton?.Invoke();
			} else if (Input.GetMouseButtonUp(0)) {
				OnLeftMouseButtonUp?.Invoke();
			} else if (Input.GetMouseButtonDown(1)) {
				OnRightMouseButtonDown?.Invoke();
			} else if (Input.GetMouseButton(1)) {
				OnRightMouseButton?.Invoke();
			} else if (Input.GetMouseButtonUp(1)) {
				OnRightMouseButtonUp?.Invoke();
			}
		}
	}
}