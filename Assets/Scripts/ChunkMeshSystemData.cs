using Unity.Entities;
using UnityEngine;

namespace Minecraft {
    public class ChunkMeshSystemData : IComponentData {
        public Material OpaqueMaterial;
        public Material TransparentMaterial;
    }
}