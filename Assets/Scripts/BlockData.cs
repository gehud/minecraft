using UnityEngine;

namespace Minecraft {
    [CreateAssetMenu]
    public class BlockData : ScriptableObject {
        public BlockTexturingData TexturingData => texturingData;
        [SerializeField] private BlockTexturingData texturingData;

        public MaterialType MaterialType => materialType;
        [SerializeField] private MaterialType materialType;

        public bool IsSolid => isSolid;
        [SerializeField] private bool isSolid = true;

        public bool IsLiquid => isLiquid;
        [SerializeField] private bool isLiquid = false;

        public bool IsTransparent => isTransparent;
        [SerializeField] private bool isTransparent = false;

        public LightColor Emission => emission;
        [SerializeField] private LightColor emission;
    }
}