using UnityEngine;

namespace Minecraft {
    [CreateAssetMenu]
    public class Block : ScriptableObject {
        public BlockTexturingData TexturingData => texturingData;
        [SerializeField] private BlockTexturingData texturingData;

        public bool IsSolid => isSolid;
        [SerializeField] private bool isSolid = true;

        public bool IsLiquid => isLiquid;
        [SerializeField] private bool isLiquid = false;

		public bool IsVegetation => isVegetation;
		[SerializeField] private bool isVegetation = false;

		public LightColor Emission => emission;
        [SerializeField] private LightColor emission;

        public bool IsTransparent => isTransparent;
        [SerializeField] private bool isTransparent = false;

        public bool IsTwoSided => isTwoSided;
        [SerializeField]
        private bool isTwoSided = false;

        public byte Absorption => absorption;
		[SerializeField, Range(LightMap.MIN, LightMap.MAX)] private byte absorption;
	}
}