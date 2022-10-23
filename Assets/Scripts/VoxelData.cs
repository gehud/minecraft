using System;
using UnityEngine;

namespace Minecraft
{
    [Serializable]
    public class VoxelData
    {
        public Vector2Int RightAtlasCoordinate;
        public Vector2Int LeftAtlasCoordinate;
        public Vector2Int TopAtlasCoordinate;
        public Vector2Int BottomAtlasCoordinate;
        public Vector2Int FrontAtlasCoordinate;
        public Vector2Int BackAtlasCoordinate;
        public MaterialType MaterialType;
        public bool IsSolid = true;
        public bool IsTransparent = false;
        public LightColor Emission;
    }
}