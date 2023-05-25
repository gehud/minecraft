using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Minecraft.Utilities {
	public static class ChunkUtility {
        public static void For(Action<int, int, int> action) {
            for (int x = 0; x < Chunk.SIZE; x++)
                for (int y = 0; y < Chunk.SIZE; y++)
                    for (int z = 0; z < Chunk.SIZE; z++)
                        action(x, y, z);
        }

		public static void ParallelFor(Action<Vector3Int> action) {
            Parallel.For(0, Chunk.VOLUME, (index, state) => {
				var coordinate = Array3DUtility.To3D(index, Chunk.SIZE, Chunk.SIZE);
				action(coordinate);
			});
        }
    }
}