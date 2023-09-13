using Unity.Collections;
using Unity.Entities;

namespace Minecraft.Components {
	public struct ChunkLoadData : IComponentData {
		public NativeList<ChunkLoadDescription> Data;
	}
}