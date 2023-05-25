using UnityEngine;
using Zenject;

namespace Minecraft {
	public class LightSolverInstaller : MonoInstaller {
		[SerializeField]
		private LightSolver instance;

		public override void InstallBindings() {
			Container.Bind<LightSolver>().FromInstance(instance).AsSingle().NonLazy();
		}
	}
}