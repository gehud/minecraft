using UnityEngine;
using Zenject;

namespace Minecraft {
	public class BlockDataManagerInstaller : MonoInstaller {
		[SerializeField]
		private BlockDataManager blockDataManager;

		public override void InstallBindings() {
			Container
				.Bind<BlockDataManager>()
				.FromInstance(blockDataManager)
				.AsSingle()
				.NonLazy();
		}
	}
}