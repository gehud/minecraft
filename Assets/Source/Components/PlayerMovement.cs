using Unity.Entities;

namespace Minecraft.Components {
    public struct PlayerMovement : IComponentData {
        public float Speed;
        public Entity OrientationSource;
        public float JumpHeight;
    }
}