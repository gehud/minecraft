using UnityEngine;
using UnityEngine.SceneManagement;

namespace Minecraft {
	public class SceneLoader : MonoBehaviour {
		public void LoadScene(string name) {
			SceneManager.LoadScene(name);
		}
	}
}