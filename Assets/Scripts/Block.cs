using UnityEngine;

namespace Minecraft {
    [CreateAssetMenu]
    public class Block : ScriptableObject {
        public Vector2Int RightFace => rightFace;
        [SerializeField] private Vector2Int rightFace;

        public Vector2Int LeftFace => leftFace;
        [SerializeField] private Vector2Int leftFace;

        public Vector2Int TopFace => topFace;
        [SerializeField] private Vector2Int topFace;

        public Vector2Int BottomFace => bottomFace;
        [SerializeField] private Vector2Int bottomFace;

        public Vector2Int FrontFace => frontFace;
        [SerializeField] private Vector2Int frontFace;

        public Vector2Int BackFace => backFace;
        [SerializeField] private Vector2Int backFace;

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