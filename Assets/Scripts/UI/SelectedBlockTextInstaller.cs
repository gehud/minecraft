using UnityEngine;
using Zenject;

namespace Minecraft.UI {
	public class SelectedBlockTextInstaller : MonoInstaller {
		[SerializeField]
		private SelectedBlockText instance;

		public override void InstallBindings() {
			Container.Bind<SelectedBlockText>().FromInstance(instance).AsSingle().NonLazy();
		}
	}
}