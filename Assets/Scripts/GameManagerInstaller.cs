using UnityEngine;
using Zenject;

namespace Minecraft {
	public class GameManagerInstaller : MonoInstaller {
		[SerializeField]
		private GameManager gameManager;

		public override void InstallBindings() {
			Container
				.Bind<GameManager>()
				.FromInstance(gameManager)
				.AsSingle()
				.NonLazy();
		}
	}
}