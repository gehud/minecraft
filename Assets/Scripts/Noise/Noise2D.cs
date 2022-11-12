using UnityEngine;

namespace Minecraft.Noise {
    public abstract class Noise2D : ScriptableObject {
        public abstract float Min { get; }
        public abstract float Max { get; }
        public abstract float Sample(float x, float y, float offsetX = 0, float offsetY = 0);
    }
}