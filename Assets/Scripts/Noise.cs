using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Minecraft {
    public struct Noise : IDisposable {
        public int3 Offset;
        public float3 Scale;
        public int Octaves;
        public float Lacunarity;
        public float Persistance;
        public NativeCurve Modification;

        public Noise(NoiseSettings settings, Allocator allocator) {
            Offset = settings.Offset;
            Scale = settings.Scale;
            Octaves = settings.Octaves;
            Lacunarity = settings.Lacunarity;
            Persistance = settings.Persistance;
            Modification = new NativeCurve(settings.Modification, allocator);
        }

        public float Sample2D(float x, float y) {
            var result = 0.0f;
            var frequency = 1.0f;
            var amplitude = 1.0f;
            var amplitudeSum = 0.0f;

            for (int i = 0; i < Octaves; i++) {
                var position = new float2 {
                    x = (x + Offset.x) * Scale.x * frequency,
                    y = (y + Offset.y) * Scale.y * frequency
                };

                result += amplitude * (noise.cnoise(position) + 1.0f) / 2.0f;
                amplitudeSum += amplitude;
                amplitude *= Persistance;
                frequency *= Lacunarity;
            }

            return Modification.Evaluate(result / amplitudeSum);
        }

        public void Dispose() {
            Modification.Dispose();
        }
    }
}