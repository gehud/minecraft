using Unity.Entities;

namespace Minecraft.Player {
    public struct PlayerMovement : IComponentData {
        public float Speed;
        public Entity OrientationSource;
        public float JumpHeight;
    }
}