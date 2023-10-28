using System;
using UnityEngine;

namespace Minecraft {
    [Serializable]
    public struct LightColor {
        [Range(Light.Min, Light.Max)]
        public byte Red;
        [Range(Light.Min, Light.Max)]
        public byte Green;
        [Range(Light.Min, Light.Max)]
        public byte Blue;
    }
}