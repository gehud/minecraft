using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft {
    [RequireComponent(typeof(MeshCollider))]
    public class ChunkCollider : MonoBehaviour {
        [SerializeField]
        private MeshCollider meshCollider;

        private Mesh mesh;

        public void UpdateMesh(ConcurrentDictionary<MaterialType, MeshData> meshData) {
            List<Vector3> vertices = new();
            List<ushort> indices = new();
            foreach (var pair in meshData) {
                int colliderVertexCount = vertices.Count;
                for (int i = 0; i < pair.Value.ColliderIndices.Count; i++) {
                    indices.Add((ushort)(pair.Value.ColliderIndices[i] + colliderVertexCount));
                }
                vertices.AddRange(pair.Value.ColliderVertices);
            }

            mesh.SetVertexBufferParams(vertices.Count,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));
            mesh.SetVertexBufferData(vertices, 0, 0, vertices.Count);
            mesh.SetIndexBufferParams(indices.Count, IndexFormat.UInt16);
            mesh.SetIndexBufferData(indices, 0, 0, indices.Count);
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indices.Count));
            meshCollider.sharedMesh = mesh;
        }

        private void Awake() {
            mesh = new();
        }
    }
}