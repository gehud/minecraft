using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Minecraft
{
    public struct VoxelMapJob : IJobParallelFor, IDisposable
    {
        private Vector3Int chunkCoordinate;
        public NativeArray<VoxelType> Result;

        public static VoxelMapJob Create(Vector3Int chunkCoordinate, Allocator allocator)
        {
            return new VoxelMapJob()
            {
                chunkCoordinate = chunkCoordinate,
                Result = new NativeArray<VoxelType>(Chunk.VOLUME, allocator)
            };
        }

        public void Execute(int index)
        {
            int z = index / (Chunk.SIZE * Chunk.SIZE);
            index -= z * Chunk.SIZE * Chunk.SIZE;
            int y = index / Chunk.SIZE;
            int x = index % Chunk.SIZE;
            Vector3Int globalVoxelCoordinate = CoordinateUtility
                .ToGlobal(chunkCoordinate, new Vector3Int(x, y, z));
            if (globalVoxelCoordinate.y < 32)
                Result[(z * Chunk.SIZE * Chunk.SIZE) + (y * Chunk.SIZE) + x] = VoxelType.Stone;
            else
                Result[(z * Chunk.SIZE * Chunk.SIZE) + (y * Chunk.SIZE) + x] = VoxelType.Air;
        }

        public JobHandle Schedule(JobHandle dependsOn = default)
        {
            return this.Schedule(Chunk.VOLUME, Chunk.SIZE, dependsOn);
        }

        public void Dispose()
        {
            Result.Dispose();
        }
    }
}