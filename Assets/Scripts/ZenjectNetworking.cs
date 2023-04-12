using Minecraft.Extensions.Zenject;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Minecraft {
	public class ZenjectPrefubInstanceHandler : INetworkPrefabInstanceHandler {
		private readonly GameObject prefub;
		private readonly DiContainer container;

		public ZenjectPrefubInstanceHandler(GameObject prefub, DiContainer container) {
			this.prefub = prefub;
			this.container = container;
		}

		public void Destroy(NetworkObject networkObject) {
			Object.Destroy(networkObject.gameObject);
		}

		public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation) {
			var parameters = new GameObjectCreationParameters { 
				Name = $"{prefub.name}_OwnedBy_{ownerClientId}",
				Position = position,
				Rotation = rotation
			};

			return container.InstantiateNetworkPrefub(prefub, parameters);
		}
	}

	[RequireComponent(typeof(NetworkManager))]
	public class ZenjectNetworking : MonoBehaviour {
		[Inject]
		private readonly DiContainer container;

		[SerializeField]
		private List<GameObject> prefabs;

		private NetworkManager networkManager;

		private void Awake() {
			networkManager = GetComponent<NetworkManager>();
			foreach (var prefab in prefabs) {
				networkManager.PrefabHandler.AddHandler(prefab, new ZenjectPrefubInstanceHandler(prefab, container));
			}
		}
	}
}