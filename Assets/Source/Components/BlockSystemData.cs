using System;
using Unity.Collections;
using Unity.Entities;

namespace Minecraft.Components {
    public struct BlockSystemData : IComponentData, IDisposable {
        public NativeArray<Block> Blocks;

        public void Dispose() {
            Blocks.Dispose();
        }
    }
}