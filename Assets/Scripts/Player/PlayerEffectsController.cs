using Minecraft.Utilities;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft.Player {
    public class PlayerEffectsController : MonoBehaviour {
        [SerializeField]
        private Volume underWaterVolume;
        [SerializeField]
        private float waterEffectHeight = 1.8f;

        private World world;

        private void Start() {
            world = World.Instance;
        }

        private void Update() {
            Vector3Int blockCoordinate = CoordinateUtility.ToCoordinate(transform.position + Vector3.up * waterEffectHeight);
            if (world.GetVoxel(blockCoordinate) == BlockType.Water) {
                underWaterVolume.enabled= true;
            } else {
                underWaterVolume.enabled= false;
            }
        }
    }
}