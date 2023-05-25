using UnityEngine;
using Zenject;

namespace Minecraft {
	public class WorldInstaller : MonoInstaller {
		[SerializeField]
		private World instance;

		public override void InstallBindings() {
			Container.Bind<World>().FromInstance(instance).AsSingle().NonLazy();
		}
	}
}