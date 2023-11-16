using System;
using Unity.Collections;
using Unity.Entities;

namespace Minecraft.Components {
    public struct ChunkMeshData : IComponentData, IDisposable {
        public NativeArray<Vertex> Vertices;
        public NativeArray<ushort> OpaqueIndices;
        public NativeArray<ushort> TransparentIndices;

        public void Dispose() {
            Vertices.Dispose();
            OpaqueIndices.Dispose();
            TransparentIndices.Dispose();
        }
    }
}