using Minecraft.Components;
using Unity.Entities;
using UnityEngine;

namespace Minecraft.Authorings {
    public class PlayerMovementAuthoring : MonoBehaviour {
        [SerializeField]
        public float speed = 15.0f;
        [SerializeField]
        private float jumpHeight = 1.0f;
        [SerializeField]
        public GameObject orientationSource;

        private class Baker : Baker<PlayerMovementAuthoring> {
            public override void Bake(PlayerMovementAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PlayerMovement {
                    Speed = authoring.speed,
                    OrientationSource = GetEntity(authoring.orientationSource, TransformUsageFlags.Dynamic),
                    JumpHeight = authoring.jumpHeight,
                });
            }
        }
    }
}