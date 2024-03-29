﻿using UnityEngine;
using Zenject;

namespace Minecraft {
	public class WorldInstaller : MonoInstaller {
		[SerializeField]
		private World world;

		public override void InstallBindings() {
			Container
				.Bind<World>()
				.FromInstance(world)
				.AsSingle()
				.NonLazy();
		}
	}
}