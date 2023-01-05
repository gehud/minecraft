using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft {
    [RequireComponent(typeof(MeshCollider))]
    public class ChunkCollider : MonoBehaviour {
        [SerializeField]
        private MeshCollider meshCollider;

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

            var mesh = new Mesh();
            mesh.Clear();
            mesh.SetVertexBufferParams(vertices.Count,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));
            mesh.SetVertexBufferData(vertices, 0, 0, vertices.Count);
            mesh.SetIndexBufferParams(indices.Count, IndexFormat.UInt16);
            mesh.SetIndexBufferData(indices, 0, 0, indices.Count);
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indices.Count));
			Vector3 center = Vector3.one * Chunk.SIZE / 2.0f;
			Vector3 size = Vector3.one * Chunk.SIZE;
			mesh.bounds = new Bounds(center, size);
			mesh.RecalculateNormals();
            mesh.UploadMeshData(true);
            meshCollider.sharedMesh = mesh;
        }
    }
}