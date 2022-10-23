using UnityEngine;

namespace Minecraft.Noise
{
    [CreateAssetMenu(menuName = "Noise/2D/Flexible")]
    public class FlexibleNoise2D : OctaveNoise2D
    {
        public AnimationCurve Modification = new();

        public override float Sample(float x, float y, float offsetX = 0, float offsetY = 0)
        {
            return Modification.Evaluate(base.Sample(x, y, offsetX, offsetY));
        }
    }
}