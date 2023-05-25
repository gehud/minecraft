using UnityEngine;
using Zenject;

namespace Minecraft.Physics {
	public class PhysicsWorldInstaller : MonoInstaller {
		[SerializeField] private PhysicsWorld instance;

		public override void InstallBindings() {
			Container.Bind<PhysicsWorld>().FromInstance(instance).AsSingle().NonLazy();
		}
	}
}