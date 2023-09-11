using Unity.Entities;

namespace Minecraft.Components {
	public struct ChunkBufferingResizeRequest : IComponentData {
		public int NewDrawDistance;
	}
}