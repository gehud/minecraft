using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Components {
	public struct Chunk : IComponentData, IDisposable {
		public const int SIZE = 16;
		public const int VOLUME = SIZE * SIZE * SIZE;

		public int3 Coordinate;
		public NativeArray<Voxel> Voxels;

		public void Dispose() {
			Voxels.Dispose();
		}
	}
}