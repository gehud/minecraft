using UnityEngine;
using Zenject;

namespace Minecraft {
	public class SaveManagerInstaller : MonoInstaller {
		[SerializeField]
		private SaveManager saveManager;

		public override void InstallBindings() {
			Container
				.Bind<SaveManager>()
				.FromInstance(saveManager)
				.AsSingle()
				.NonLazy();
		}
	}
}