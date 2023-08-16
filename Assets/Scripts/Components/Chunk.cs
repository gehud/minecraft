using Unity.Collections;
using Unity.Entities;

namespace Minecraft.Components {
	public struct Chunk : IComponentData {
		public const int SIZE = 16;
		public const int VOLUME = SIZE * SIZE * SIZE;

		/// <summary>
		/// Claster includes this chunk and chunks around. Claster is 3x3 array;
		/// </summary>
		public NativeArray<Entity> Claster;
		public NativeArray<Voxel> Voxels;
	}
}