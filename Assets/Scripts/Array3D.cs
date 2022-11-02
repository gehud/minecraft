using UnityEngine;

namespace Minecraft {
    public class Array3D<T> {
        private readonly int x, y, z;
        private readonly T[] data;

        public Array3D(int x, int y, int z) {
            this.x = x;
            this.y = y;
            this.z = z;
            data = new T[this.x * this.y * this.z];
        }

        public T this[int x, int y, int z] {
            get => data[(z * this.x * this.y) + (y * this.x) + x];
            set => data[(z * this.x * this.y) + (y * this.x) + x] = value;
        }

        public T this[Vector3Int coordinate] {
            get => this[coordinate.x, coordinate.y, coordinate.z];
            set => this[coordinate.x, coordinate.y, coordinate.z] = value;
        }

        public void CopyTo(Array3D<T> other) {
            data.CopyTo(other.data, 0);
        }
    }
}