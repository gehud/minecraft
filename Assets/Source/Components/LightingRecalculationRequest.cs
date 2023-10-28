using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Components {
    public struct LightingRecalculationRequest : IComponentData {
        public int2 Column;
    }
}