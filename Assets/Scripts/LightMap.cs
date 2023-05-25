using System.Runtime.CompilerServices;
using UnityEngine;

namespace Minecraft {
    public class LightMap : Array3D<ushort> {
        public const int RED = 0;
        public const int GREEN = 1;
        public const int BLUE = 2;
        public const int SUN = 3;

        public const byte MIN = 0;
        public const byte MAX = 15;

        public LightMap() : base(Chunk.SIZE, Chunk.SIZE, Chunk.SIZE) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Get(int x, int y, int z, int chanel) {
            return (byte)((Data[z * XY + y * X + x] >> (chanel << 2)) & 0xF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Get(Vector3Int coordinate, int chanel) {
			return (byte)((Data[coordinate.z * XY + coordinate.y * X + coordinate.x] >> (chanel << 2)) & 0xF);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int x, int y, int z, int chanel, byte value) {
            Data[z * XY + y * X + x] = (ushort)((Data[z * XY + y * X + x] & (0xFFFF & (~(0xF << (chanel * 4))))) | (value << (chanel << 2)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(Vector3Int coordinate, int chanel, byte value) {
			Data[coordinate.z * XY + coordinate.y * X + coordinate.x] = (ushort)((Data[coordinate.z * XY + coordinate.y * X + coordinate.x] & (0xFFFF & (~(0xF << (chanel * 4))))) | (value << (chanel << 2)));
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte GetRed(int x, int y, int z) {
            return (byte)(Data[z * XY + y * X + x] & 0xF);
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte GetRed(Vector3Int coordinate) {
			return (byte)(Data[coordinate.z * XY + coordinate.y * X + coordinate.x] & 0xF);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRed(int x, int y, int z, byte value) {
            Data[z * XY + y * X + x] = (ushort)((Data[z * XY + y * X + x] & 0xFFF0) | value);
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRed(Vector3Int coordinate, byte value) {
			Data[coordinate.z * XY + coordinate.y * X + coordinate.x] = (ushort)((Data[coordinate.z * XY + coordinate.y * X + coordinate.x] & 0xFFF0) | value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetGreen(int x, int y, int z) {
            return (byte)((Data[z * XY + y * X + x] >> 4) & 0xF);
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetGreen(Vector3Int coordinate) {
			return (byte)((Data[coordinate.z * XY + coordinate.y * X + coordinate.x] >> 4) & 0xF);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetGreen(int x, int y, int z, byte value) {
            Data[z * XY + y * X + x] = (ushort)((Data[z * XY + y * X + x] & 0xFF0F) | (value << 4));
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetGreen(Vector3Int coordinate, byte value) {
			Data[coordinate.z * XY + coordinate.y * X + coordinate.x] = (ushort)((Data[coordinate.z * XY + coordinate.y * X + coordinate.x] & 0xFF0F) | (value << 4));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetBlue(int x, int y, int z) {
            return (byte)((Data[z * XY + y * X + x] >> 8) & 0xF);
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetBlue(Vector3Int coordinate) {
			return (byte)((Data[coordinate.z * XY + coordinate.y * X + coordinate.x] >> 8) & 0xF);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBlue(int x, int y, int z, byte value) {
            Data[z * XY + y * X + x] = (ushort)((Data[z * XY + y * X + x] & 0xF0FF) | (value << 8));
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBlue(Vector3Int coordinate, byte value) {
			Data[coordinate.z * XY + coordinate.y * X + coordinate.x] = (ushort)((Data[coordinate.z * XY + coordinate.y * X + coordinate.x] & 0xF0FF) | (value << 8));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetSun(int x, int y, int z) {
            return (byte)((Data[z * XY + y * X + x] >> 12) & 0xF);
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetSun(Vector3Int coordinate) {
			return (byte)((Data[coordinate.z * XY + coordinate.y * X + coordinate.x] >> 12) & 0xF);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSun(int x, int y, int z, byte value) {
            Data[z * XY + y * X + x] = (ushort)((Data[z * XY + y * X + x] & 0x0FFF) | (value << 12));
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSun(Vector3Int coordinate, byte value) {
			Data[coordinate.z * XY + coordinate.y * X + coordinate.x] = (ushort)((Data[coordinate.z * XY + coordinate.y * X + coordinate.x] & 0x0FFF) | (value << 12));
		}
    }
}