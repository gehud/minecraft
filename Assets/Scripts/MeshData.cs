using System.Collections.Concurrent;
using UnityEngine;

namespace Minecraft
{
    public class MeshData
    {
        public ConcurrentBag<Vertex> Vertices = new();
        public ConcurrentBag<ushort> Indices = new();
        public ConcurrentBag<Vector3> ColliderVertices = new();
        public ConcurrentBag<ushort> ColliderIndices = new();
    }
}