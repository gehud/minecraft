using UnityEngine;
using Zenject;

namespace Minecraft {
	public class MaterialProviderInstaller : MonoInstaller {
		[SerializeField]
		private MaterialProvider materialManager;

		public override void InstallBindings() {
			Container
				.Bind<MaterialProvider>()
				.FromInstance(materialManager)
				.AsSingle()
				.NonLazy();
		}
	}
}