using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Components {
    public struct PhysicsSystemData : IComponentData {
        public float3 Gravity;
        public float ContactOffset;
        public SystemHandle ChunkBufferingSystem;
        public SystemHandle BlockSystem;
    }
}