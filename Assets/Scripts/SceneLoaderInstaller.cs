using UnityEngine;
using Zenject;

namespace Minecraft {
	public class SceneLoaderInstaller : MonoInstaller {
		[SerializeField]
		private SceneLoader loader;

		public override void InstallBindings() {
			Container
				.Bind<SceneLoader>()
				.FromInstance(loader)
				.AsSingle()
				.NonLazy();
		}
	}
}