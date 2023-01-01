using System.Runtime.CompilerServices;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Get(int x, int y, int z, BlockType type) {
            var unit = Data[z * XY + y * X + x];
            return (byte)(unit.Type == type ? unit.Amount : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte Get(Vector3Int coordinate, BlockType type) {
			var unit = Data[coordinate.z * XY + coordinate.y * X + coordinate.x];
			return (byte)(unit.Type == type ? unit.Amount : 0);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Get(int x, int y, int z) {
            return Data[z * XY + y * X + x].Amount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte Get(Vector3Int coordinate) {
			return Data[coordinate.z * XY + coordinate.y * X + coordinate.x].Amount;
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int x, int y, int z, BlockType type, byte value) {
            Data[z * XY + y * X + x] = new LiquidData(type, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(Vector3Int coordinate, BlockType type, byte value) {
			Data[coordinate.z * XY + coordinate.y * X + coordinate.x] = new LiquidData(type, value);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int x, int y, int z, byte value) {
            Data[z * XY + y * X + x] = new LiquidData(Data[z * XY + y * X + x].Type, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Set(Vector3Int coordinate, byte value) {
			Data[coordinate.z * XY + coordinate.y * X + coordinate.x] = new LiquidData(Data[coordinate.z * XY + coordinate.y * X + coordinate.x].Type, value);
		}
	}
}