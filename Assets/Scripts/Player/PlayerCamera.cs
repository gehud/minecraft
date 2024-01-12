using Unity.Entities;

namespace Minecraft.Player {
    public struct PlayerCamera : IComponentData {
        public float Sensitivity;
        public float Yaw;
        public float Pitch;
        public Entity OrientationTarget;
    }
}