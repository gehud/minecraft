using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Components {
    public struct ChunkLoadingRequest : IComponentData {
        public int2 NewCenter;
    }
}