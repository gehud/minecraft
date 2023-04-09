using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Minecraft.UI;

namespace Minecraft {
	public class SaveController : MonoBehaviour {
		[SerializeField]
		private SaveView view;

		[Inject]
		private readonly SaveManager saveManager;

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
			saveManager.LoadSave(saveView.Model.Name);
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
