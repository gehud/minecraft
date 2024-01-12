using Unity.Mathematics;

namespace Minecraft.Utilities {
    public static class IndexUtility {
        public static int ToIndex(int x, int y, int z, int xMax, int yMax) {
            return z * xMax * yMax + y * xMax + x;
        }

        public static int ToIndex(in int3 coordinate, int xMax, int yMax) {
            return ToIndex(coordinate.x, coordinate.y, coordinate.z, xMax, yMax);
        }

        public static int ToIndex(int x, int y, int xMax) {
            return y * xMax + x;
        }

        public static int3 ToCoordinate(int index, int xMax, int yMax) {
            int z = index / (xMax * yMax);
            index -= z * xMax * yMax;
            int y = index / xMax;
            int x = index % xMax;
            return new int3(x, y, z);
        }
    }
}