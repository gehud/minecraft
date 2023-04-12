using UnityEngine;
using Zenject;

namespace Minecraft.UI {
	public class UIControllerInstaller : MonoInstaller {
		[SerializeField]
		private UIController instance;

		public override void InstallBindings() {
			Container.Bind<UIController>().FromInstance(instance).AsSingle().NonLazy();
		}
	}
}