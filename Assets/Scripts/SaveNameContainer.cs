using Zenject;

namespace Minecraft {
	public class SaveNameContainer : MonoInstaller, ISaveNameContainer {
		public string Name { get; set; }

		public override void InstallBindings() {
			Container.Bind<ISaveNameContainer>().FromInstance(this).AsSingle().NonLazy();
		}
	}
}