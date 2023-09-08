using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Components {
	public struct ChunkBuffer : IComponentData, IDisposable {
		public const int HEIGHT = 16;

		public int2 Center;
		public int DrawDistance;
		public int ChunksSize;
		public NativeArray<Entity> Chunks;
		public NativeArray<Entity> ChunksBuffer;

		public void Dispose() {
			Chunks.Dispose();
			ChunksBuffer.Dispose();
		}
	}
}