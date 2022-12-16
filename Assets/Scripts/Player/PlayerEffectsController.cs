using Minecraft.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace Minecraft.Player {
    public class PlayerEffectsController : MonoBehaviour {
        [SerializeField]
        private Volume underWaterVolume;
        [SerializeField]
        private float waterEffectHeight = 1.8f;

        [Inject]
        private World World { get; } 

        private void Update() {
            Vector3Int blockCoordinate = CoordinateUtility.ToCoordinate(transform.position + Vector3.up * waterEffectHeight);
            if (World.GetBlock(blockCoordinate) == BlockType.Water) {
                underWaterVolume.enabled= true;
            } else {
                underWaterVolume.enabled= false;
            }
        }
    }
}