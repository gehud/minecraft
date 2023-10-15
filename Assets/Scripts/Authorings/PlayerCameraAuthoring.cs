using Minecraft.Components;
using Unity.Entities;
using UnityEngine;

namespace Minecraft.Authorings {
    public class PlayerCameraAuthoring : MonoBehaviour {
        [SerializeField]
        private float sensitivity = 0.15f;
        [SerializeField]
        public GameObject orientationTarget;

        private class Baker : Baker<PlayerCameraAuthoring> {
            public override void Bake(PlayerCameraAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PlayerCamera {
                    Sensitivity = authoring.sensitivity,
                    OrientationTarget = GetEntity(authoring.orientationTarget, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}