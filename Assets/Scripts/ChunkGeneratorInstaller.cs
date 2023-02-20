using UnityEngine;
using Zenject;

namespace Minecraft {
	public class ChunkGeneratorInstaller : MonoInstaller {
		[SerializeField]
		private ChunkGenerator chunkDataGenerator;

		public override void InstallBindings() {
			Container
				.Bind<ChunkGenerator>()
				.FromInstance(chunkDataGenerator)
				.AsSingle()
				.NonLazy();
		}
	}
}