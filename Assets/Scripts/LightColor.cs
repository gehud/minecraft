using System;
using UnityEngine;

namespace Minecraft {
    [Serializable]
    public struct LightColor : IEquatable<LightColor> {
        public static LightColor Zero => new(0, 0, 0);

        [Range(LightMap.MIN, LightMap.MAX)] public byte R;
        [Range(LightMap.MIN, LightMap.MAX)] public byte G;
        [Range(LightMap.MIN, LightMap.MAX)] public byte B;

        public LightColor(byte r, byte g, byte b) {
            R = r;
            G = g;
            B = b;
        }

        public override bool Equals(object obj) {
            return obj is LightColor other &&
                   R == other.R &&
                   G == other.G &&
                   B == other.B;
        }

        public bool Equals(LightColor other) {
            return R == other.R &&
                   G == other.G &&
                   B == other.B;
        }

        public override int GetHashCode() {
            return HashCode.Combine(R, G, B);
        }

        /*        public static bool operator == (LightColor left, LightColor right)
                {
                    return left.Equals(right);
                }

                public static bool operator != (LightColor left, LightColor right)
                {
                    return !left.Equals(right);
                }*/
    }
}