using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Components {
	public struct ChunkInitializer : IComponentData {
		public int3 Coordinate;
	}
}