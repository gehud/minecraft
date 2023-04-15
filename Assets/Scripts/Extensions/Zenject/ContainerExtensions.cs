using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Minecraft.Extensions.Zenject {
	public static class ContainerExtensions {
		public static NetworkObject InstantiateNetworkPrefub(this DiContainer container, GameObject prefub, GameObjectCreationParameters gameObjectBindInfo) {
			var isActive = prefub.activeSelf;
			prefub.SetActive(false);
			var instance = container.InstantiatePrefab(prefub, gameObjectBindInfo);
			prefub.SetActive(isActive);
			instance.SetActive(isActive);
			return instance.GetComponent<NetworkObject>();
		}

		public static NetworkObject InstantiateNetworkPrefub(this DiContainer container, GameObject prefub) {
			return InstantiateNetworkPrefub(container, prefub, GameObjectCreationParameters.Default);
		}
	}
}