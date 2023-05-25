using UnityEngine;
using Zenject;

namespace Minecraft {
	public class BlockProviderInstaller : MonoInstaller {
		[SerializeField]
		private BlockProvider instance;

		public override void InstallBindings() {
			Container.Bind<BlockProvider>().FromInstance(instance).AsSingle().NonLazy();
		}
	}
}