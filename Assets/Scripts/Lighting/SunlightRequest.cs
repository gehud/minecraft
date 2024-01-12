using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Lighting {
    public struct SunlightRequest : IComponentData {
        public int2 Column;
    }
}