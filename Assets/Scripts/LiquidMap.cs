using UnityEngine;

namespace Minecraft {
    public struct LiquidData {
        public static LiquidData Empty => default;

        public BlockType Type;
        public byte Amount;

        public LiquidData(BlockType type, byte amount) {
            Type = type;
            Amount = amount;
        }
    }

    public class LiquidMap : Array3D<LiquidData> {
        public const byte MIN = 0;
        public const byte MAX = 8;

        public LiquidMap() : base(Chunk.SIZE, Chunk.SIZE, Chunk.SIZE) { }

        public byte Get(int x, int y, int z, BlockType type) {
            var unit = this[x, y, z];
            return (byte)(unit.Type == type ? unit.Amount : 0);
        }

        public byte Get(Vector3Int coordinate, BlockType type) {
            return Get(coordinate.x, coordinate.y, coordinate.z, type);
        }

        public void Set(int x, int y, int z, BlockType type, byte value) {
            this[x, y, z] = new LiquidData(type, value);
        }

        public void Set(Vector3Int coordinate, BlockType type, byte value) {
            Set(coordinate.x, coordinate.y, coordinate.z, type, value);
        }
    }
}