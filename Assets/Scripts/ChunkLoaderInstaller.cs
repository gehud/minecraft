using UnityEngine;
using Zenject;

namespace Minecraft {
	public class ChunkLoaderInstaller : MonoInstaller {
		[SerializeField]
		private ChunkLoader chunkLoader;

		public override void InstallBindings() {
			Container
				.Bind<ChunkLoader>()
				.FromInstance(chunkLoader)
				.AsSingle()
				.NonLazy();
		}
	}
}