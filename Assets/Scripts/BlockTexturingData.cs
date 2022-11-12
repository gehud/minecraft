using System;
using UnityEngine;

namespace Minecraft {
    [Serializable]
    public struct BlockTexturingData {
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
    }
}