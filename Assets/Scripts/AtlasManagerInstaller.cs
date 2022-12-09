using UnityEngine;
using Zenject;

namespace Minecraft {
	public class AtlasManagerInstaller : MonoInstaller {
		[SerializeField]
		private AtlasManager atlasManager;

		public override void InstallBindings() {
			Container
				.Bind<AtlasManager>()
				.FromInstance(atlasManager)
				.AsSingle()
				.NonLazy();
		}
	}
}