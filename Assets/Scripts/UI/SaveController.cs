using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Minecraft.UI {
	public class SaveController : MonoBehaviour {
		[SerializeField]
		private SaveView view;

		[Inject]
		private readonly SceneLoader sceneLoader;

		public void CreateEmpty() {
			Create(new Save { 
				Name = "Untitled",
				Icon = null
			});
		}

		public void Create(Save save) {
			var view = Instantiate(this.view, transform);
			if (save.Icon)
				view.GetComponentInChildren<Image>().sprite = save.Icon;
			string[] files = Directory.GetFiles(Application.persistentDataPath + "/saves");
			foreach (string file in files) {
				var fileName = Path.GetFileNameWithoutExtension(file);
				if (fileName == save.Name) {
					if (int.TryParse(fileName[^1].ToString(), out int lastNumber)) {
						var fileNameBase = fileName[..^1];
						save.Name = fileNameBase + (++lastNumber);
					} else { 
						save.Name = fileName + "_1";
					}
				}
			}
			view.GetComponentInChildren<TMP_Text>().text = save.Name;
			view.Model = save;
			string path = Application.persistentDataPath + "/saves/" + save.Name + ".world";
			using var fs = new FileStream(path, FileMode.Create);
		}

		private void Add(Save save) {
			var view = Instantiate(this.view, transform);
			if (save.Icon)
				view.GetComponentInChildren<Image>().sprite = save.Icon;
			view.GetComponentInChildren<TMP_Text>().text = save.Name;
			view.Model = save;
		}

		private void Play(SaveView saveView) {
			PlayerPrefs.SetString("SelectedWorld", saveView.Model.Name);
			sceneLoader.LoadScene("Overworld");
		}

		private void Delete(SaveView saveView) {
			string path = Application.persistentDataPath + "/saves/" + saveView.Model.Name + ".world";
			File.Delete(path);
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
			string dataPath = Application.persistentDataPath + "/saves";
			if (!Directory.Exists(dataPath))
				Directory.CreateDirectory(dataPath);
			string[] files = Directory.GetFiles(Application.persistentDataPath + "/saves", "*.world");
			foreach (string file in files) {
				string fileName = Path.GetFileNameWithoutExtension(file);
				Add(new Save {
					Name = fileName
				});
			}
		}
	}
}
