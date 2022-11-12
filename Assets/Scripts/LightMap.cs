using UnityEngine;

namespace Minecraft {
    public enum LightChanel {
        Red,
        Green,
        Blue,
        Sun,
    }

    public class LightMap : Array3D<ushort> {
        public const byte MIN = 0;
        public const byte MAX = 15;

        public LightMap() : base(Chunk.SIZE, Chunk.SIZE, Chunk.SIZE) { }

        public byte Get(int x, int y, int z, LightChanel chanel) {
            return (byte)((this[x, y, z] >> ((int)chanel << 2)) & 0xF);
        }

        public byte Get(Vector3Int coordinate, LightChanel chanel) {
            return Get(coordinate.x, coordinate.y, coordinate.z, chanel);
        }

        public void Set(int x, int y, int z, LightChanel chanel, byte value) {
            this[x, y, z] = (ushort)((this[x, y, z] & (0xFFFF & (~(0xF << ((int)chanel * 4))))) | (value << ((int)chanel << 2)));
        }

        public void Set(Vector3Int coordinate, LightChanel chanel, byte value) {
            Set(coordinate.x, coordinate.y, coordinate.z, chanel, value);
        }

        public byte GetRed(int x, int y, int z) {
            return (byte)(this[x, y, z] & 0xF);
        }

        public byte GetRed(Vector3Int coordinate) {
            return GetRed(coordinate.x, coordinate.y, coordinate.z);
        }

        public void SetRed(int x, int y, int z, byte value) {
            this[x, y, z] = (ushort)((this[x, y, z] & 0xFFF0) | value);
        }

        public void SetRed(Vector3Int coordinate, byte value) {
            SetRed(coordinate.x, coordinate.y, coordinate.z, value);
        }

        public byte GetGreen(int x, int y, int z) {
            return (byte)((this[x, y, z] >> 4) & 0xF);
        }

        public byte GetGreen(Vector3Int coordinate) {
            return GetGreen(coordinate.x, coordinate.y, coordinate.z);
        }

        public void SetGreen(int x, int y, int z, byte value) {
            this[x, y, z] = (ushort)((this[x, y, z] & 0xFF0F) | (value << 4));
        }

        public void SetGreen(Vector3Int coordinate, byte value) {
            SetGreen(coordinate.x, coordinate.y, coordinate.z, value);
        }

        public byte GetBlue(int x, int y, int z) {
            return (byte)((this[x, y, z] >> 8) & 0xF);
        }

        public byte GetBlue(Vector3Int coordinate) {
            return GetBlue(coordinate.x, coordinate.y, coordinate.z);
        }

        public void SetBlue(int x, int y, int z, byte value) {
            this[x, y, z] = (ushort)((this[x, y, z] & 0xF0FF) | (value << 8));
        }

        public void SetBlue(Vector3Int coordinate, byte value) {
            SetBlue(coordinate.x, coordinate.y, coordinate.z, value);
        }

        public byte GetSun(int x, int y, int z) {
            return (byte)((this[x, y, z] >> 12) & 0xF);
        }

        public int GetSun(Vector3Int coordinate) {
            return GetSun(coordinate.x, coordinate.y, coordinate.z);
        }

        public void SetSun(int x, int y, int z, byte value) {
            this[x, y, z] = (ushort)((this[x, y, z] & 0x0FFF) | (value << 12));
        }

        public void SetSun(Vector3Int coordinate, byte value) {
            SetSun(coordinate.x, coordinate.y, coordinate.z, value);
        }
    }
}