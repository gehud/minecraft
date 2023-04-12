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
		private readonly IConnectionRoleContainer connectionRoleContainer;

		[SerializeField]
		private int spawnPlayerHeight = 160;

		private MovementController player;

		public void SetPlayer(MovementController player) {
			this.player = player;
		}

		private void OnWorldCreate() {
			if (player == null)
				return;

			player.enabled = true;
		}

		private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) {
			response.Approved = true;
			response.CreatePlayerObject = true;
			response.Position = Vector3.up * spawnPlayerHeight;
			response.Rotation = Quaternion.identity;
		}

		private void Start() {
			NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
			switch (connectionRoleContainer.Role) {
				case ConnectionRoles.Host:
					NetworkManager.Singleton.StartHost();
					break;
				case ConnectionRoles.Server:
					NetworkManager.Singleton.StartServer();
					break;
				case ConnectionRoles.Client:
					NetworkManager.Singleton.StartClient();
					break;
			}
		}

		private void OnEnable() {
			chunkLoader.OnWorldCreate += OnWorldCreate;
		}

		private void OnDisable() {
			chunkLoader.OnWorldCreate -= OnWorldCreate;
		}

		public override void OnNetworkDespawn() {
			NetworkManager.Singleton.Shutdown();
		}
	}
}