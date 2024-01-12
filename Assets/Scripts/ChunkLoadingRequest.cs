using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft {
    public struct ChunkLoadingRequest : IComponentData {
        public int2 NewCenter;
    }
}