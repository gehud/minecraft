using System.Linq;
using UnityEngine;

namespace Minecraft.Noise {
    [CreateAssetMenu(menuName = "Noise/2D/Flexible")]
    public class FlexibleNoise2D : OctaveNoise2D {
        public override float Max => Modification.keys.Length == 0 ? 1.0f : Modification.keys.Max(k => k.value);

        public override float Min => Modification.keys.Length == 0 ? 0.0f : Modification.keys.Min(k => k.value);

        public AnimationCurve Modification = new();

        public override float Sample(float x, float y, float offsetX = 0, float offsetY = 0) {
            return Modification.Evaluate(base.Sample(x, y, offsetX, offsetY));
        }
    }
}