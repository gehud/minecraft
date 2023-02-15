using UnityEngine;
using Zenject;

namespace Minecraft {
	public class PhysicsSolverInstaller : MonoInstaller {
		[SerializeField] private PhysicsSolver physicsSolver;

		public override void InstallBindings() {
			Container
				.Bind<PhysicsSolver>()
				.FromInstance(physicsSolver)
				.AsSingle()
				.NonLazy();
		}
	}
}