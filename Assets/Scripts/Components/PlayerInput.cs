using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Components {
    public struct PlayerInput : IComponentData {
        public float2 Movement;
        public float2 Look;
        public float Air;
        public bool IsJump;
        public bool IsSprint;

        public bool IsAttack;
        public bool IsDefend;
    }
}