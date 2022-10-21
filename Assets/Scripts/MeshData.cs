using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
    public class MeshData
    {
        public List<Vertex> Vertices = new();
        public List<ushort> Indices = new();
        public List<Vector3> ColliderVertices = new();
        public List<ushort> ColliderIndices = new();
    }
}