using Unity.Entities;
using UnityEngine;

namespace Minecraft.Components {
    public class ChunkMeshSystemData : IComponentData {
        public Material OpaqueMaterial;
        public Material TransparentMaterial;
    }
}