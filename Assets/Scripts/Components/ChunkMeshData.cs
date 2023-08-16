using System;
using Unity.Collections;
using Unity.Entities;

namespace Minecraft.Components {
	public struct ChunkMeshData : IComponentData, IDisposable {
		public NativeArray<Vertex> Vertices;
		public NativeArray<ushort> Indices;

		public void Dispose() {
			Vertices.Dispose();
			Indices.Dispose();
		}
	}
}