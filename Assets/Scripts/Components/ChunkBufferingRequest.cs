using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Components {
	public readonly struct ChunkBufferingRequest : IComponentData {
		public readonly int2 NewCenter;
		public readonly int NewDrawDistance;

		public ChunkBufferingRequest(int2 newCenter, int newDrawDistance) {
			NewCenter = newCenter;
			NewDrawDistance = newDrawDistance;
		}
	}
}