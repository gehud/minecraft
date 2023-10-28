using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Components {
    public struct ChunkBufferingSystemData : IComponentData, IDisposable {
        public int2 Center;
        public int Height;
        public int DrawDistance;

        public int ChunksSize;
        public NativeArray<Entity> Chunks;
        public NativeArray<Entity> ChunksBuffer;

        public void Dispose() {
            Chunks.Dispose();
            ChunksBuffer.Dispose();
        }
    }
}