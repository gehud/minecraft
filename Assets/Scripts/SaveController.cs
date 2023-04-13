using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Minecraft.UI;

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
			var view = Instantiate(this.view, transform);
			if (save.Icon)
				view.GetComponentInChildren<Image>().sprite = save.Icon;
			view.GetComponentInChildren<TMP_Text>().text = saveManager.CreateSave(save.Name);
			view.Model = save;
		}

		private void Add(Save save) {
			var view = Instantiate(this.view, transform);
			if (save.Icon)
				view.GetComponentInChildren<Image>().sprite = save.Icon;
			view.GetComponentInChildren<TMP_Text>().text = save.Name;
			view.Model = save;
		}

		private void Play(SaveView saveView) {
			switch (gameType) {
				case GameTypes.Local:
					saveManager.LoadGame(saveView.Model.Name);
					break;
				case GameTypes.Multyplayer:
					saveManager.HostGame(saveView.Model.Name);
					break;
			}
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
