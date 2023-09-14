using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Components {
	public struct ChunkLoadData : IComponentData {
		public NativeList<ChunkLoadDescription> Data;
		public int2 LastPlayerColumn;
	}
}