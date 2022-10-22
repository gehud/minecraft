using System;
using Unity.Collections;
using UnityEngine;

namespace Minecraft
{
    public struct VoxelMap : IDisposable
    {
        private NativeArray<VoxelType> data; 

        public VoxelMap(Allocator allocator)
        {
            data = new NativeArray<VoxelType>(Chunk.VOLUME, allocator);
        }

        public VoxelType this[int x, int y, int z]
        {
            get => data[(z * Chunk.SIZE * Chunk.SIZE) + (y * Chunk.SIZE) + x];
            set => data[(z * Chunk.SIZE * Chunk.SIZE) + (y * Chunk.SIZE) + x] = value;
        }

        public VoxelType this[Vector3Int coordinate]
        {
            get => this[coordinate.x, coordinate.y, coordinate.z];
            set => this[coordinate.x, coordinate.y, coordinate.z] = value;
        }

        public void SetData(NativeArray<VoxelType> data)
        {
            if (data.Length != Chunk.VOLUME)
                throw new ArgumentException("Array length is incorrect.");
            this.data = data;
        }

        public NativeArray<VoxelType> GetData()
        {
            return data;
        }

        public void Dispose()
        {
            data.Dispose(); 
        }
    }
}