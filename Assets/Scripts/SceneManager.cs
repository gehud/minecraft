using UnityEngine;
using Zenject;

namespace Minecraft {
	public interface ISceneManager {
		void LoadScene(string name);

		AsyncOperation LoadSceneAsync(string name);
	}

	public class SceneManager : MonoInstaller, ISceneManager {
		public void LoadScene(string name) {            
			UnityEngine.SceneManagement.SceneManager.LoadScene(name);
		}

		public AsyncOperation LoadSceneAsync(string name) {
			return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(name);
		}

		public override void InstallBindings() {
			Container.Bind<ISceneManager>().FromInstance(this).AsSingle().NonLazy();
		}
	}
}