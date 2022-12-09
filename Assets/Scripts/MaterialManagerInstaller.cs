using UnityEngine;
using Zenject;

namespace Minecraft {
	public class MaterialManagerInstaller : MonoInstaller {
		[SerializeField]
		private MaterialManager materialManager;

		public override void InstallBindings() {
			Container
				.Bind<MaterialManager>()
				.FromInstance(materialManager)
				.AsSingle()
				.NonLazy();
		}
	}
}