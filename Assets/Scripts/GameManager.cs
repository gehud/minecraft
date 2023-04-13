using Minecraft.Extensions.Zenject;
using Minecraft.Player;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Minecraft {
	public class GameManager : NetworkBehaviour {
        public void ApplicationQuit() => Application.Quit();

		[Inject]
		private readonly ChunkLoader chunkLoader;

		[Inject]
		private readonly DiContainer diContainer;

		[Inject]
		private readonly ISavePayload savePayload;

		[SerializeField]
		private int spawnPlayerHeight = 160;

		[SerializeField]
		private GameObject player;

		private MovementController movementController;

		private void OnWorldCreate() {
			if (movementController != null)
				movementController.enabled = true;
		}

		private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) {
			response.Approved = true;
			response.CreatePlayerObject = true;
			response.Position = Vector3.up * spawnPlayerHeight;
			response.Rotation = Quaternion.identity;
		}

		private void Start() {
			NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
			switch (savePayload.Role) {
				case ConnectionRoles.Host:
					NetworkManager.Singleton.StartHost();
					break;
				case ConnectionRoles.Server:
					NetworkManager.Singleton.StartServer();
					break;
				case ConnectionRoles.Client:
					NetworkManager.Singleton.StartClient();
					break;
				case ConnectionRoles.None:
					diContainer.InstantiateNetworkPrefub(player);
					break;
			}
		}

		private void OnPlayerSpawn(GameObject player) {
			this.movementController = player.GetComponent<MovementController>();	
		}

		private void OnEnable() {
			chunkLoader.OnWorldCreate += OnWorldCreate;
			PlayerEvents.OnSpawn += OnPlayerSpawn;
		}

		private void OnDisable() {
			chunkLoader.OnWorldCreate -= OnWorldCreate;
			PlayerEvents.OnSpawn -= OnPlayerSpawn;
		}

		public override void OnNetworkDespawn() {
			NetworkManager.Singleton.Shutdown();
		}
	}
}