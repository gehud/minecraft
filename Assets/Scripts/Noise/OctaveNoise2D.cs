using UnityEngine;

namespace Minecraft.Noise
{
    [CreateAssetMenu(menuName = "Noise/2D/Octave")]
    public class OctaveNoise2D : SimpleNoise2D
    {
        [Min(1)] public int Octaves = 3;
        [Min(0)] public float Lacunarity = 2;
        [Range(0, 1)] public float Persistance = 0.5f;

        public override float Sample(float x, float y, float offsetX = 0, float offsetY = 0)
        {
            float result = 0;
            float frequency = 1;
            float amplitude = 1;
            float amplitudeSum = 0;

            for (int i = 0; i < Octaves; i++)
            {
                result += base.Sample(x * frequency, y * frequency, offsetX, offsetY) * amplitude;
                amplitudeSum += amplitude;
                amplitude *= Persistance;
                frequency *= Lacunarity;
            }

            result /= amplitudeSum;

            return result;
        }
    }
}