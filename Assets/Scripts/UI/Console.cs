using System;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace Minecraft {
	public class Console : MonoBehaviour {
		[SerializeField]
		private TMP_Text messagesText;
		[SerializeField]
		private TMP_InputField commandField;

		private void OnLogMessageRecieved(string message, string stackTrace, LogType type) {
			messagesText.text += message + Environment.NewLine;
		}

		private IEnumerator StartInput() {
			yield return null;
			commandField.Select();
			commandField.ActivateInputField();
		}

		private void OnCommandSubmit(string text) {
			var commandRegex = new Regex(@"^\/(\w+)(\s+.*)");
			var commandMatch = commandRegex.Match(text);
			var argumentsRegex = new Regex(@"\s+(.+)");
			if (!commandMatch.Success)
				return;
			var command = commandMatch.Groups[1].Value;
			if (command == "say") {
				if (commandMatch.Groups.Count > 2) {
					var arguments = commandMatch.Groups[2].Value;
					var argumentsMatch = argumentsRegex.Match(arguments);
					if (argumentsMatch.Success) {
						Debug.Log(argumentsMatch.Groups[1].Value);
					}
				}
			}
		}

		private void Awake() {
			commandField.onSubmit.AddListener(OnCommandSubmit);
		}

		private void OnEnable() {
			Application.logMessageReceivedThreaded += OnLogMessageRecieved;
			StartCoroutine(StartInput());
		}

		private void OnDisable() {
			Application.logMessageReceivedThreaded -= OnLogMessageRecieved;
		}
	}
}