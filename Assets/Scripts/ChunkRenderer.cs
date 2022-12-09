using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace Minecraft {
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ChunkRenderer : MonoBehaviour {
        [SerializeField]
        private MeshFilter meshFilter;
        [SerializeField]
        private MeshRenderer meshRenderer;

        private Mesh mesh;

        public void UpdateMesh(IDictionary<MaterialType, MeshData> meshDatas, MaterialManager materialManager) {
            mesh.Clear();

            List<SubMeshDescriptor> subMeshDescriptors = new();
            List<ushort> indices = new();
            List<Vertex> vertices = new();
            List<Material> materials = new();
            foreach (var meshData in meshDatas) {
                subMeshDescriptors.Add(new SubMeshDescriptor(indices.Count, meshData.Value.Indices.Count));
                int vertexCount = vertices.Count;
                indices.AddRange(meshData.Value.Indices.Select(i => (ushort)(i + vertexCount)));
                vertices.AddRange(meshData.Value.Vertices);
                materials.Add(materialManager.Materials[meshData.Key]);
            }

            mesh.SetVertexBufferParams(vertices.Count,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4));
            mesh.SetVertexBufferData(vertices, 0, 0, vertices.Count);
            mesh.SetIndexBufferParams(indices.Count, IndexFormat.UInt16);
            mesh.SetIndexBufferData(indices, 0, 0, indices.Count);
            mesh.SetSubMeshes(subMeshDescriptors);
            meshRenderer.materials = materials.ToArray();

            mesh.RecalculateNormals();
        }

        private void Awake() {
            mesh = new();
            meshFilter.mesh = mesh;
        }
    }
}