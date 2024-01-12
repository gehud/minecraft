using System;
using Unity.Collections;
using Unity.Entities;

namespace Minecraft {
    public struct BlockSystemData : IComponentData, IDisposable {
        public NativeArray<Block> Blocks;

        public void Dispose() {
            Blocks.Dispose();
        }
    }
}