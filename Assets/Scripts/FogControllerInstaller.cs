using UnityEngine;
using Zenject;

namespace Minecraft {
	public class FogControllerInstaller : MonoInstaller {
		[SerializeField]
		private FogController instance;

		public override void InstallBindings() {
			Container.Bind<FogController>().FromInstance(instance).AsSingle().NonLazy();
		}
	}
}