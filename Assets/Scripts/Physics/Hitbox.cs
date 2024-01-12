using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Physics {
    public struct Hitbox : IComponentData {
        public float3 Velocity;
        public bool DisableGravity;
        public AABB Bounds;
    }
}