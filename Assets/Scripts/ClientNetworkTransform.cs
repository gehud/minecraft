using Unity.Netcode.Components;
using UnityEngine;

namespace Minecraft {
	[DisallowMultipleComponent]
	public class ClientNetworkTransform : NetworkTransform {
		protected override bool OnIsServerAuthoritative() {
			return false;
		}
	}
}