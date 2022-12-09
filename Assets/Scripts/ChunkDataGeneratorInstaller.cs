using UnityEngine;
using Zenject;

namespace Minecraft {
	public class ChunkDataGeneratorInstaller : MonoInstaller {
		[SerializeField]
		private ChunkDataGenerator chunkDataGenerator;

		public override void InstallBindings() {
			Container
				.Bind<ChunkDataGenerator>()
				.FromInstance(chunkDataGenerator)
				.AsSingle()
				.NonLazy();
		}
	}
}