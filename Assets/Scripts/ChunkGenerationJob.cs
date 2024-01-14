using Minecraft.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Minecraft {
    [BurstCompile]
    public struct ChunkGenerationJob : IJobFor {
        [ReadOnly]
        public Entity Entity;
        [ReadOnly]
        public int3 Coordinate;
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<Voxel> Voxels;
        [ReadOnly]
        public Noise Continentalness;
        [ReadOnly]
        public Noise Erosion;
        [ReadOnly]
        public Noise PeaksAndValleys;
        [ReadOnly]
        public int WaterLevel;

        public void Execute(int index) {
            var localCoordinate = IndexUtility.ToCoordinate(index, Chunk.Size, Chunk.Size);
            var coordinate = Coordinate * Chunk.Size + localCoordinate;

            var continentalness = Continentalness.Sample2D(coordinate.x, coordinate.z);
            var erosion = Erosion.Sample2D(coordinate.x, coordinate.z);
            var peaksAndValleys = PeaksAndValleys.Sample2D(coordinate.x, coordinate.z);
            var result = continentalness * erosion * peaksAndValleys;

            int height = (int)result;
            if (coordinate.y <= height) {
                if (coordinate.y == height) {
                    Voxels[index] = new Voxel(BlockType.Grass);
                } else if (coordinate.y >= height - 4) {
                    Voxels[index] = new Voxel(BlockType.Dirt);
                } else {
                    Voxels[index] = new Voxel(BlockType.Stone);
                }
            } else if (coordinate.y <= WaterLevel) {
                Voxels[index] = new Voxel(BlockType.Water);
            }
        }
    }
}