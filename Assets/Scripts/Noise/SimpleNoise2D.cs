using UnityEngine;

namespace Minecraft.Noise
{
    [CreateAssetMenu(menuName = "Noise/2D/Simple")]
    public class SimpleNoise2D : Noise2D
    {
        [Min(0)] public float Scale = 1.0f;
        public Vector2 Offset = Vector2.zero;

        public override float Sample(float x, float y, float offsetX = 0, float offsetY = 0)
        {
            return Mathf.PerlinNoise((x + offsetX + Offset.x) * Scale,
                                     (y + offsetY + Offset.y) * Scale);
        }
    }
}