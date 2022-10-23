﻿using UnityEngine;

namespace Minecraft
{
    public class ChunkData
    {
        public Vector3Int Coordinate { get; set; } = Vector3Int.zero;
        public VoxelMap VoxelMap { get; set; } = new();
        public LightMap LightMap { get; set; } = new();
        public bool IsDirty { get; set; } = true;
        public bool IsComplete { get; set; } = false;
    }
}