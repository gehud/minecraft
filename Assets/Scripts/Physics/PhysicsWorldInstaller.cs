using UnityEngine;
using Zenject;

namespace Minecraft.Physics {
	public class PhysicsWorldInstaller : MonoInstaller {
		[SerializeField] private PhysicsWorld physicsSolver;

		public override void InstallBindings() {
			Container
				.Bind<PhysicsWorld>()
				.FromInstance(physicsSolver)
				.AsSingle()
				.NonLazy();
		}
	}
}