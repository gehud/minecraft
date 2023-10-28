using Unity.Entities;

namespace Minecraft.Components {
    public struct ChunkBufferingRequest : IComponentData {
        public int NewDrawDistance;
    }
}