using UnityEngine;

namespace Minecraft
{
    public class Array3D<T>
    {
        private readonly int x, y, z = 0;
        private T[] values = null;

        public Array3D(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            values = new T[this.x * this.y * this.z];
        }

        public T this[int x, int y, int z]
        {
            get => values[(z * this.x * this.y) + (y * this.x) + x];
            set => values[(z * this.x * this.y) + (y * this.x) + x] = value;
        }

        public T this[Vector3Int vector] 
        {
            get => this[vector.x, vector.y, vector.z];
            set => this[vector.x, vector.y, vector.z] = value;
        }
    }
}