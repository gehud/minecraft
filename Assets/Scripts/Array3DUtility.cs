using UnityEngine;

namespace Minecraft {
	public static class Array3DUtility {
		public static int To1D(int x, int y, int z, int xMax, int yMax) {
			return (z * xMax * yMax) + (y * xMax) + x;
		}

		public static Vector3Int To3D(int index, int xMax, int yMax) {
			int z = index / (xMax * yMax);
			index -= (z * xMax * yMax);
			int y = index / xMax;
			int x = index % xMax;
			return new Vector3Int(x, y, z);
		}
	}
}
