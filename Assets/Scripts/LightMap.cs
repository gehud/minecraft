using UnityEngine;

namespace Minecraft
{
    public class LightMap : Array3D<ushort>
    {
        public enum Chanel
        {
            Red,
            Green,
            Blue,
            Sun,
        }

        public const int MIN = 0;
        public const int MAX = 15;

        public LightMap() : base(Chunk.SIZE, Chunk.SIZE, Chunk.SIZE) { }

        public int Get(int x, int y, int z, Chanel chanel)
        {
            return (this[x, y, z] >> ((int)chanel << 2)) & 0xF;
        }

        public int Get(Vector3Int coordinate, Chanel chanel)
        {
            return Get(coordinate.x, coordinate.y, coordinate.z, chanel);
        }

        public void Set(int x, int y, int z, Chanel chanel, int value)
        {
            this[x, y, z] = (ushort)((this[x, y, z] & (0xFFFF & (~(0xF << ((int)chanel * 4))))) | (value << ((int)chanel << 2)));
        }

        public void Set(Vector3Int coordinate, Chanel chanel, int value)
        {
            Set(coordinate.x, coordinate.y, coordinate.z, chanel, value);
        }

        public int GetRed(int x, int y, int z)
        {
            return this[x, y, z] & 0xF;
        }

        public int GetRed(Vector3Int coordinate)
        {
            return GetRed(coordinate.x, coordinate.y, coordinate.z);
        }

        public void SetRed(int x, int y, int z, int value)
        {
            this[x, y, z] = (ushort)((this[x, y, z] & 0xFFF0) | value);
        }

        public void SetRed(Vector3Int coordinate, int value)
        {
            SetRed(coordinate.x, coordinate.y, coordinate.z, value);
        }

        public int GetGreen(int x, int y, int z)
        {
            return (this[x, y, z] >> 4) & 0xF;
        }

        public int GetGreen(Vector3Int coordinate)
        {
            return GetGreen(coordinate.x, coordinate.y, coordinate.z);
        }

        public void SetGreen(int x, int y, int z, int value)
        {
            this[x, y, z] = (ushort)((this[x, y, z] & 0xFF0F) | (value << 4));
        }

        public void SetGreen(Vector3Int coordinate, int value)
        {
            SetGreen(coordinate.x, coordinate.y, coordinate.z, value);
        }

        public int GetBlue(int x, int y, int z)
        {
            return (this[x, y, z] >> 8) & 0xF;
        }

        public int GetBlue(Vector3Int coordinate)
        {
            return GetBlue(coordinate.x, coordinate.y, coordinate.z);
        }

        public void SetBlue(int x, int y, int z, int value)
        {
            this[x, y, z] = (ushort)((this[x, y, z] & 0xF0FF) | (value << 8));
        }

        public void SetBlue(Vector3Int localVoxelCoordinate, int value)
        {
            SetBlue(localVoxelCoordinate.x, localVoxelCoordinate.y, localVoxelCoordinate.z, value);
        }

        public int GetSun(int x, int y, int z)
        {
            return (this[x, y, z] >> 12) & 0xF;
        }

        public int GetSun(Vector3Int localVoxelCoordinate)
        {
            return GetSun(localVoxelCoordinate.x, localVoxelCoordinate.y, localVoxelCoordinate.z);
        }

        public void SetSun(int x, int y, int z, int value)
        {
            this[x, y, z] = (ushort)((this[x, y, z] & 0x0FFF) | (value << 12));
        }

        public void SetSun(Vector3Int localVoxelCoordinate, int value)
        {
            SetSun(localVoxelCoordinate.x, localVoxelCoordinate.y, localVoxelCoordinate.z, value);
        }
    }
}