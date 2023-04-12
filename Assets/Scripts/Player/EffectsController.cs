using Minecraft.Utilities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace Minecraft.Player {
	public class EffectsController : NetworkBehaviour {
        [SerializeField]
        private Volume underWaterVolume;
        [SerializeField]
        private float waterEffectHeight = 1.8f;

        [Inject]
        private World World { get; } 

        private void Update() {
			if (!IsOwner)
				return;

			Vector3Int blockCoordinate = CoordinateUtility.ToCoordinate(transform.position + Vector3.up * waterEffectHeight);
            if (World.GetBlock(blockCoordinate) == BlockType.Water) {
                underWaterVolume.enabled = true;
            } else {
                underWaterVolume.enabled = false;
            }
        }
    }
}