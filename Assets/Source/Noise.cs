using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Minecraft {
    public struct Noise : IDisposable {
        public float Scale;
        public int Octaves;
        public float Lacunarity;
        public float Persistance;
        public NativeCurve Modification;

        public Noise(NoiseSettings settings, Allocator allocator) {
            Scale = settings.Scale;
            Octaves = settings.Octaves;
            Lacunarity = settings.Lacunarity;
            Persistance = settings.Persistance;
            Modification = new NativeCurve(settings.Modification, allocator);
        }

        public float Sample2D(float x, float y) {
            float result = 0;
            float frequency = 1;
            float amplitude = 1;
            float amplitudeSum = 0;

            for (int i = 0; i < Octaves; i++) {
                result += noise.cnoise(new float2(x * frequency * Scale, y * frequency * Scale)) * amplitude;
                amplitudeSum += amplitude;
                amplitude *= Persistance;
                frequency *= Lacunarity;
            }

            result /= amplitudeSum;

            return Modification.Evaluate(result);
        }

        public void Dispose() {
            Modification.Dispose();
        }
    }
}