using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft
{
    public class Chunk : MonoBehaviour
    {
        public const int SIZE = 16;
        public const int VOLUME = SIZE * SIZE * SIZE;

        public ChunkData Data { get; set; }

        [SerializeField] 
        private MeshFilter meshFilter;
        [SerializeField] 
        private MeshRenderer meshRenderer;
        [SerializeField] 
        private MeshCollider meshCollider;

        private Mesh renderMesh;
        private Mesh colliderMesh;

        private World world;

        public void Initialize(ChunkData data)
        {
            Data = data;
            transform.position = Data.Coordinate * SIZE;
        }

        public void UpdateMesh(IDictionary<MaterialType, MeshData> meshDatas)
        {
            renderMesh.Clear();
            colliderMesh.Clear();

            List<Vertex> vertices = new();
            List<Vector3> colliderVertices = new();
            List<ushort> indices = new();
            List<ushort> colliderIndices = new();
            List<SubMeshDescriptor> subMeshDescriptors = new();
            List<Material> materials = new();
            foreach (var meshData in meshDatas)
            {
                subMeshDescriptors.Add(new SubMeshDescriptor(indices.Count, meshData.Value.Indices.Count));
                int vertexCount = vertices.Count;
                indices.AddRange(meshData.Value.Indices.Select(i => (ushort)(i + vertexCount)));
                int colliderVertexCount = colliderVertices.Count;
                colliderIndices.AddRange(meshData.Value.ColliderIndices.Select(i => (ushort)(i + colliderVertexCount)));
                vertices.AddRange(meshData.Value.Vertices);
                colliderVertices.AddRange(meshData.Value.ColliderVertices);
                materials.Add(world.Materials[meshData.Key]);
            }

            renderMesh.SetVertexBufferParams(vertices.Count, 
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4));
            renderMesh.SetVertexBufferData(vertices, 0, 0, vertices.Count);
            renderMesh.SetIndexBufferParams(indices.Count, IndexFormat.UInt16);
            renderMesh.SetIndexBufferData(indices, 0, 0, indices.Count);
            renderMesh.SetSubMeshes(subMeshDescriptors);
            meshRenderer.materials = materials.ToArray();

            renderMesh.RecalculateNormals();

            colliderMesh.SetVertexBufferParams(colliderVertices.Count, 
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));
            colliderMesh.SetVertexBufferData(colliderVertices, 0, 0, colliderVertices.Count);
            colliderMesh.SetIndexBufferParams(colliderIndices.Count, IndexFormat.UInt16);
            colliderMesh.SetIndexBufferData(colliderIndices, 0, 0, colliderIndices.Count);
            colliderMesh.SetSubMesh(0, new SubMeshDescriptor(0, colliderIndices.Count));
            meshCollider.sharedMesh = colliderMesh;

            colliderMesh.RecalculateNormals();

            Data.IsDirty = false;
            Data.IsComplete = true;
        }

        public void UpdateMesh()
        {
            UpdateMesh(ChunkUtility.GenerateMeshData(world, Data));
        }

        public void Handle()
        {
            if (Data.IsComplete && Data.IsDirty)
                UpdateMesh();
        }

        private void Awake()
        {
            renderMesh = new();
            meshFilter.mesh = renderMesh;
            colliderMesh = new();
            world = World.Instance;
        }
    }
}