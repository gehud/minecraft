﻿using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Components {
    public struct SunlightRequest : IComponentData {
        public int2 Column;
    }
}