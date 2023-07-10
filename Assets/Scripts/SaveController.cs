using Minecraft.UI;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Minecraft {
	public class SaveController : MonoBehaviour {
		private enum GameTypes {
			Local,
			Multyplayer
		}

		private GameTypes gameType = GameTypes.Local;

		[Inject]
		private readonly SaveManager saveManager;

		[SerializeField]
		private SaveView view;
		[SerializeField]
		private RectTransform localGames;
		[SerializeField]
		private RectTransform onlineGames;

		public void MarkAsLocal() {
			gameType = GameTypes.Local;
		}

		public void MarkAsMultyplayer() {
			gameType = GameTypes.Multyplayer;
		}

		public void CreateEmpty() {
			Create(new Save { 
				Name = "New World",
				Icon = null
			});
		}

		public void Create(Save save) {
			var view = Instantiate(this.view, localGames);
			if (save.Icon)
				view.GetComponentInChildren<Image>().sprite = save.Icon;
			view.GetComponentInChildren<TMP_Text>().text = saveManager.CreateSave(save.Name);
			view.Model = save;
		}

		private void Add(Save save) {
			var view = Instantiate(this.view, localGames);
			if (save.Icon)
				view.GetComponentInChildren<Image>().sprite = save.Icon;
			view.GetComponentInChildren<TMP_Text>().text = save.Name;
			view.Model = save;
		}

		private void Play(SaveView saveView) {
			switch (gameType) {
				case GameTypes.Local:
					StartCoroutine(LaunchLocalGame(saveView));
					break;
				case GameTypes.Multyplayer:
					StartCoroutine(LaunchMultyplayerGame(saveView));
					break;
			}
		}

		private IEnumerator WaitForWorldCreation(AsyncOperation gameLoading) {
			yield return gameLoading;
		}

		private IEnumerator LaunchLocalGame(SaveView saveView) {
			var gameLoading = saveManager.LoadGameAsync(saveView.Model.Name);
			yield return StartCoroutine(WaitForWorldCreation(gameLoading));
		}

		private IEnumerator LaunchMultyplayerGame(SaveView saveView) {
			var gameLoading = saveManager.HostGameAsync(saveView.Model.Name);
			yield return StartCoroutine(WaitForWorldCreation(gameLoading));
		}

		private void Delete(SaveView saveView) {
			saveManager.DeleteSave(saveView.Model.Name);
			Destroy(saveView.gameObject);
		}

		private void OnEnable() {
			SaveView.OnPlay += Play;
			SaveView.OnDelete += Delete;
		}

		private void OnDisable() {
			SaveView.OnPlay -= Play;
			SaveView.OnDelete -= Delete;
		}

		private void Start() {
			var saveNames = saveManager.GetAllSaveNames();
			foreach (var saveName in saveNames) {
				Add(new Save { 
					Name = saveName,
				});
			}
		}
	}
}
