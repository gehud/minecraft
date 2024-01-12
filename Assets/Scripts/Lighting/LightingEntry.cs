using Unity.Mathematics;

namespace Minecraft.Lighting {
    public struct LightingEntry {
        public int3 Coordinate;
        public byte Level;

        public LightingEntry(int3 coordinate, byte level) {
            Coordinate = coordinate;
            Level = level;
        }

        public LightingEntry(int x, int y, int z, byte level) : this(new int3(x, y, z), level) { }
    }
}