using UnityEngine;
using Zenject;

namespace Minecraft {
	public class BlockProviderInstaller : MonoInstaller {
		[SerializeField]
		private BlockProvider blockDataManager;

		public override void InstallBindings() {
			Container
				.Bind<BlockProvider>()
				.FromInstance(blockDataManager)
				.AsSingle()
				.NonLazy();
		}
	}
}