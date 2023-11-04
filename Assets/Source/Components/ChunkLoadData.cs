using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Components {
    public struct ChunkLoadData : IComponentData {
        public int Sequence;
        public NativeList<ChunkLoadDescription> Data;
        public int2 LastPlayerColumn;
    }
}