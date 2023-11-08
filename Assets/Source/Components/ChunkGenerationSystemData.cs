using System;
using Unity.Entities;

namespace Minecraft.Components {
    public struct ChunkGenerationSystemData : IComponentData, IDisposable {
        public int HeightOffset;
        public Noise Continentalness;
        public Noise Erosion;
        public Noise PeaksAndValleys;

        public void Dispose() {
            Continentalness.Dispose();
            Erosion.Dispose();
            PeaksAndValleys.Dispose();
        }
    }
}