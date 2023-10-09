using Unity.Entities;

namespace Minecraft.Components {
    public struct ChunkMeshDataSystemData : IComponentData {
        public SystemHandle ChunkBufferingSystem;
    }
}