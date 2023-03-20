using UnityEngine;

namespace Minecraft {
    public class Array3D<T> {
        protected readonly T[] Data;
        protected readonly int X, XY;

        public Array3D(int x, int y, int z) {
            X = x;
            XY = x * y;
            Data = new T[x * y * z];
        }

        public T this[int i] {
            get => Data[i];
            set => Data[i] = value;
        }

        public T this[int x, int y, int z] {
            get => Data[z * XY + y * X + x];
            set => Data[z * XY + y * X + x] = value;
        }

        public T this[Vector3Int coordinate] {
            get => Data[coordinate.z * XY + coordinate.y * X + coordinate.x];
            set => Data[coordinate.z * XY + coordinate.y * X + coordinate.x] = value;
        }

        public void CopyTo(Array3D<T> other) {
            Data.CopyTo(other.Data, 0);
        }
    }
}