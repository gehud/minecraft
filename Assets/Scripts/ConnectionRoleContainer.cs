using Zenject;

namespace Minecraft {
	public class ConnectionRoleContainer : MonoInstaller, IConnectionRoleContainer {
		public ConnectionRoles Role { get; set; } = ConnectionRoles.Host;

		public override void InstallBindings() {
			Container.Bind<IConnectionRoleContainer>().FromInstance(this).AsSingle().NonLazy();
		}
	}
}