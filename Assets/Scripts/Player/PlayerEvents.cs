using System;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Minecraft {
	public class PlayerEvents : NetworkBehaviour {
		public static event Action<GameObject> OnSpawn;

		[Inject]
		private readonly ISavePayload savePayload;

		private void Start() {
			if (savePayload.Role != ConnectionRoles.None)
				return;

			OnSpawn?.Invoke(gameObject);
		}

		public override void OnNetworkSpawn() {
			base.OnNetworkSpawn();

			if (!IsOwner)
				return;

			OnSpawn?.Invoke(gameObject);
		}
	}
}