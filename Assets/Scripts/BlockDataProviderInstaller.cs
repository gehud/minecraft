using UnityEngine;
using Zenject;

namespace Minecraft {
	public class BlockDataProviderInstaller : MonoInstaller {
		[SerializeField]
		private BlockDataProvider blockDataManager;

		public override void InstallBindings() {
			Container
				.Bind<BlockDataProvider>()
				.FromInstance(blockDataManager)
				.AsSingle()
				.NonLazy();
		}
	}
}