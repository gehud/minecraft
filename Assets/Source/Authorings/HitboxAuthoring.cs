using Minecraft.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Minecraft.Authorings {
    public class HitboxAuthoring : MonoBehaviour {
        [SerializeField]
        private AABB bounds;

        private class Baker : Baker<HitboxAuthoring> {
            public override void Bake(HitboxAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Hitbox {
                    Bounds = authoring.bounds
                });
            }
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + (Vector3)bounds.Center, bounds.Size);
        }
    }
}