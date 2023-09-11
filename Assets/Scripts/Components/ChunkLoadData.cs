using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Components {
	public struct ChunkLoadData : IComponentData {
		public NativeList<int3> Chunks;
		public NativeList<int3> Renderers;
	}
}