using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Minecraft {
    public struct Noise : IDisposable {
        public int3 Offset;
        public int Octaves;
        public float Lacunarity;
        public float Persistance;
        public NativeCurve Modification;

        public Noise(NoiseSettings settings, Allocator allocator) {
            Offset = settings.Offset;
            Octaves = settings.Octaves;
            Lacunarity = settings.Lacunarity;
            Persistance = settings.Persistance;
            Modification = new NativeCurve(settings.Modification, allocator);
        }

        public float Sample2D(int x, int y) {
            float result = 0;
            float frequency = 1;
            float amplitude = 1;
            float amplitudeSum = 0;

            for (int i = 0; i < Octaves; i++) {
                var position = new float2() {
                    x = (x + int.MaxValue + Offset.x) * frequency / uint.MaxValue,
                    y = (y + int.MaxValue + Offset.y) * frequency / uint.MaxValue
                };
                result += noise.cnoise(position) * amplitude;
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