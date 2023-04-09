using UnityEngine;
using Zenject;

namespace Minecraft {
	public class AtlasProviderInstaller : MonoInstaller {
		[SerializeField]
		private AtlasProvider atlasManager;

		public override void InstallBindings() {
			Container
				.Bind<AtlasProvider>()
				.FromInstance(atlasManager)
				.AsSingle()
				.NonLazy();
		}
	}
}