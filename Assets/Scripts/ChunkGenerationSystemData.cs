using System;
using Unity.Entities;

namespace Minecraft {
    public struct ChunkGenerationSystemData : IComponentData, IDisposable {
        public Noise Continentalness;
        public Noise Erosion;
        public Noise PeaksAndValleys;
        public int WaterLevel;

        public void Dispose() {
            Continentalness.Dispose();
            Erosion.Dispose();
            PeaksAndValleys.Dispose();
        }
    }
}