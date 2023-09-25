using Unity.Mathematics;

namespace Minecraft.Utilities {
	public static class Array3DUtility {
		public static int To1D(int x, int y, int z, int xMax, int yMax) {
			return z * xMax * yMax + y * xMax + x;
		}

		public static int To1D(int3 coordinate, int xMax, int yMax) {
			return coordinate.z * xMax * yMax + coordinate.y * xMax + coordinate.x;
		}

		public static int3 To3D(int index, int xMax, int yMax) {
			int z = index / (xMax * yMax);
			index -= z * xMax * yMax;
			int y = index / xMax;
			int x = index % xMax;
			return new int3(x, y, z);
		}
	}
}