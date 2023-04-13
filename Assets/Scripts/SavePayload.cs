using Zenject;

namespace Minecraft {
	public class SavePayload : MonoInstaller, ISavePayload {
		public string Name { get; set; }

		public ConnectionRoles Role { get; set; }

		public override void InstallBindings() {
			Container.Bind<ISavePayload>().FromInstance(this).AsSingle().NonLazy();
		}
	}
}