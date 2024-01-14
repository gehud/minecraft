using Minecraft.Utilities;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Minecraft.Lighting {
    [BurstCompile]
    public struct SunlightCalculationJob : IJob, IDisposable {
        [ReadOnly]
        public NativeArray<Block> Blocks;
        [ReadOnly]
        public int2 Column;
        [ReadOnly]
        public int BufferHeight;
        [ReadOnly]
        public int ClasterHeight;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<NativeArray<Voxel>> Claster;

        public NativeQueue<LightingEntry> AddQueues;
        public NativeQueue<LightingEntry> RemoveQueues;

        private static readonly int3[] blockSides = {
            new( 0,  0,  1),
            new( 0,  0, -1),
            new( 0,  1,  0),
            new( 0, -1,  0),
            new( 1,  0,  0),
            new(-1,  0,  0),
        };

        public void Execute() {
            var startX = Column.x * Chunk.Size;
            var endX = Column.x * Chunk.Size + Chunk.Size;
            var startZ = Column.y * Chunk.Size;
            var endZ = Column.y * Chunk.Size + Chunk.Size;
            for (int x = startX; x < endX; x++) {
                for (int z = startZ; z < endZ; z++) {
                    for (int y = BufferHeight * Chunk.Size - 1; y >= 0; y--) {
                        var voxelCoordinate = new int3(x, y, z);
                        var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
                        GetVoxels(chunkCoordinate, out var voxels);
                        var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
                        var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
                        var voxel = voxels[index];
                        var block = Blocks[(int)voxel.Type];
                        if (!block.IsTransparent || block.Absorption > 0) {
                            break;
                        }

                        voxel.Light.Set(LightChanel.Sun, Light.Max);
                        voxels[index] = voxel;
                        var entry = new LightingEntry(voxelCoordinate, Light.Max);
                        AddQueues.Enqueue(entry);
                    }
                }
            }

            while (RemoveQueues.TryDequeue(out var entry)) {
                for (int i = 0; i < blockSides.Length; i++) {
                    var voxelCoordinate = entry.Coordinate + blockSides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
                    GetVoxels(chunkCoordinate, out var voxels);
                    if (!voxels.IsCreated) {
                        continue;
                    }

                    var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
                    var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
                    var voxel = voxels[index];
                    var level = voxel.Light.Get(LightChanel.Sun);
                    var blockType = voxels[index].Type;
                    var absorption = Blocks[(int)blockType].Absorption;
                    if (level != 0 && level == entry.Level - absorption - 1) {
                        var removeEntry = new LightingEntry(voxelCoordinate, level);
                        RemoveQueues.Enqueue(removeEntry);
                        voxel.Light.Set(LightChanel.Sun, Light.Min);
                        voxels[index] = voxel;
                    } else if (level >= entry.Level) {
                        var addEntry = new LightingEntry(voxelCoordinate, level);
                        AddQueues.Enqueue(addEntry);
                    }
                }
            }

            while (AddQueues.TryDequeue(out var entry)) {
                if (entry.Level <= 1) {
                    continue;
                }

                for (int i = 0; i < blockSides.Length; i++) {
                    var voxelCoordinate = entry.Coordinate + blockSides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
                    GetVoxels(chunkCoordinate, out var voxels);
                    if (!voxels.IsCreated) {
                        continue;
                    }

                    var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
                    var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
                    var voxel = voxels[index];
                    var level = voxel.Light.Get(LightChanel.Sun);
                    var blockType = voxels[index].Type;
                    var absorption = Blocks[(int)blockType].Absorption;
                    if (Blocks[(int)blockType].IsTransparent && level + absorption + 1 < entry.Level) {
                        var newLevel = (byte)(entry.Level - absorption - 1);
                        voxel.Light.Set(LightChanel.Sun, newLevel);
                        voxels[index] = voxel;
                        var addEntry = new LightingEntry(voxelCoordinate, newLevel);
                        AddQueues.Enqueue(addEntry);
                    }
                }
            }
        }

        private void GetVoxels(in int3 chunkCoordinate, out NativeArray<Voxel> voxels) {
            var clasterCoordinate = new int3 {
                x = chunkCoordinate.x - Column.x + 1,
                y = chunkCoordinate.y + 1,
                z = chunkCoordinate.z - Column.y + 1
            };

            var clasterIndex = IndexUtility.ToIndex(clasterCoordinate, 3, ClasterHeight);
            voxels = Claster[clasterIndex];
        }

        public void Dispose() {
            Claster.Dispose();
            AddQueues.Dispose();
            RemoveQueues.Dispose();
        }
    }
}