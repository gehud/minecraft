using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft {
    public struct ChunkSpawnRequest : IComponentData {
        public int3 Coordinate;
        public bool HasRenderer;
    }
}