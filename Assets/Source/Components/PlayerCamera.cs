using Unity.Entities;

namespace Minecraft.Components {
    public struct PlayerCamera : IComponentData {
        public float Sensitivity;
        public float Yaw;
        public float Pitch;
        public Entity OrientationTarget;
    }
}