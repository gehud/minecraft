using Unity.Entities;

namespace Minecraft {
    public struct ChunkBufferingRequest : IComponentData {
        public int NewDrawDistance;
    }
}