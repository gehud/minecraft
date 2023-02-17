using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft {
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ChunkRenderer : MonoBehaviour {
		public Chunk Data { get; set; }

        [SerializeField]
        private MeshFilter meshFilter;
        [SerializeField]
        private MeshRenderer meshRenderer;

        private Mesh mesh;

		public void Initialize(Chunk data) {
			Data = data;
			transform.position = data.Coordinate * Chunk.SIZE;
		}

		public void UpdateMesh(ConcurrentDictionary<MaterialType, MeshData> meshData, MaterialManager materialManager) {
            List<SubMeshDescriptor> subMeshDescriptors = new();
            List<ushort> indices = new();
            List<Vertex> vertices = new();
            List<Material> materials = new();
            foreach (var pair in meshData) {
                subMeshDescriptors.Add(new SubMeshDescriptor(indices.Count, pair.Value.Indices.Count));
                int vertexCount = vertices.Count;
                for (int i = 0; i < pair.Value.Indices.Count; i++) {
                    indices.Add((ushort)(pair.Value.Indices[i] + vertexCount));
                }
                vertices.AddRange(pair.Value.Vertices);
                materials.Add(materialManager.Materials[pair.Key]);
            }

            mesh.SetVertexBufferParams(vertices.Count,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 1));
            mesh.SetVertexBufferData(vertices, 0, 0, vertices.Count);
            mesh.SetIndexBufferParams(indices.Count, IndexFormat.UInt16);
            mesh.SetIndexBufferData(indices, 0, 0, indices.Count);
            mesh.SetSubMeshes(subMeshDescriptors);

            Vector3 center = Vector3.one * Chunk.SIZE / 2.0f;
            Vector3 size = Vector3.one * Chunk.SIZE;
            mesh.bounds = new Bounds(center, size);

            meshRenderer.materials = materials.ToArray();

			Data.IsDirty = false;
			Data.IsComplete = true;
		}

        private void Awake() {
            mesh = new();
            meshFilter.sharedMesh = mesh;
        }
    }
}