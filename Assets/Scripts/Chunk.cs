using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft {
    public struct Chunk : IComponentData, IDisposable {
        public const int Size = 16;
        public const int Volume = Size * Size * Size;

        public int3 Coordinate;
        public NativeArray<Voxel> Voxels;

        public void Dispose() {
            Voxels.Dispose();
        }
    }
}