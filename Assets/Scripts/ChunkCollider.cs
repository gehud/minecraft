using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft {
    [RequireComponent(typeof(MeshCollider))]
    public class ChunkCollider : MonoBehaviour {
        [SerializeField]
        private MeshCollider meshCollider;

        private Mesh mesh;

        public void UpdateMesh(IDictionary<MaterialType, MeshData> meshDatas) {
            List<Vector3> vertices = new();
            List<ushort> indices = new();
            foreach (var meshData in meshDatas) {
                int colliderVertexCount = vertices.Count;
                indices.AddRange(meshData.Value.ColliderIndices.Select(i => (ushort)(i + colliderVertexCount)));
                vertices.AddRange(meshData.Value.ColliderVertices);
            }
            mesh.Clear();
            mesh.SetVertexBufferParams(vertices.Count,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));
            mesh.SetVertexBufferData(vertices, 0, 0, vertices.Count);
            mesh.SetIndexBufferParams(indices.Count, IndexFormat.UInt16);
            mesh.SetIndexBufferData(indices, 0, 0, indices.Count);
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indices.Count));
            meshCollider.sharedMesh = mesh;

            mesh.RecalculateNormals();
        }

        private void Awake() {
            mesh = new();
        }
    }
}