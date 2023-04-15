using Unity.Netcode.Components;
using UnityEngine;

namespace Minecraft {
	[DisallowMultipleComponent]
	public class OwnerNetworkAnimator : NetworkAnimator {
		protected override bool OnIsServerAuthoritative() {
			return false;
		}
	}
}