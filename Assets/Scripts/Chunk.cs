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

        private Vector3Int coordinate;
        public Vector3Int Coordinate
        {
            get => coordinate;
            set 
            { 
                coordinate = value;
                transform.position = coordinate * SIZE;
            }
        }

        public VoxelMap VoxelMap { get; set; } = new();


        private bool isDirty = true;
        public bool IsDirty => isDirty;

        [SerializeField] 
        private MeshFilter meshFilter;
        [SerializeField] 
        private MeshRenderer meshRenderer;
        [SerializeField] 
        private MeshCollider meshCollider;

        Mesh renderMesh;
        Mesh colliderMesh;

        private World world;

        public void UpdateMesh(Dictionary<MaterialType, MeshData> meshDatas)
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
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2));
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

            isDirty = false;
        }

        public void UpdateMesh()
        {
            VoxelType GetVoxel(int x, int y, int z)
            {
                Vector3Int globalVoxelCoordinate = CoordinateUtility.ToGlobal(Coordinate, new Vector3Int(x, y, z));
                return world.GetVoxel(globalVoxelCoordinate);
            }

            bool IsVoxelSolid(int x, int y, int z)
            {
                return world.Voxels[GetVoxel(x, y, z)].IsSolid;
            }

            bool IsVoxelTransparent(int x, int y, int z)
            {
                return world.Voxels[GetVoxel(x, y, z)].IsTransparent;
            }

            Dictionary<MaterialType, MeshData> meshDatas = new();

            ChunkUtility.ForEachVoxel((x, y, z) =>
            {
                VoxelType voxelType = VoxelMap[x, y, z];
                if (voxelType != VoxelType.Air)
                {
                    MaterialType materialType = world.Voxels[voxelType].MaterialType;
                    if (!meshDatas.ContainsKey(materialType))
                        meshDatas.Add(materialType, new MeshData());

                    float atlasStep = 16.0f / 256.0f;
                    float uvOffset = 0.0f;
                    bool isSolid = IsVoxelSolid(x, y, z);
                    var meshData = meshDatas[materialType];

                    void AddIndices()
                    {
                        int vertexCount = meshData.Vertices.Count;
                        meshData.Indices.Add((ushort)(0 + vertexCount));
                        meshData.Indices.Add((ushort)(1 + vertexCount));
                        meshData.Indices.Add((ushort)(2 + vertexCount));
                        meshData.Indices.Add((ushort)(0 + vertexCount));
                        meshData.Indices.Add((ushort)(2 + vertexCount));
                        meshData.Indices.Add((ushort)(3 + vertexCount));
                    }

                    void AddColliderIndices()
                    {
                        int vertexCount = meshData.ColliderVertices.Count;
                        meshData.ColliderIndices.Add((ushort)(0 + vertexCount));
                        meshData.ColliderIndices.Add((ushort)(1 + vertexCount));
                        meshData.ColliderIndices.Add((ushort)(2 + vertexCount));
                        meshData.ColliderIndices.Add((ushort)(0 + vertexCount));
                        meshData.ColliderIndices.Add((ushort)(2 + vertexCount));
                        meshData.ColliderIndices.Add((ushort)(3 + vertexCount));
                    }

                    // Right face.
                    if (IsVoxelTransparent(x + 1, y, z))
                    {
                        Vector2 rightAtlasPosition = (Vector2)world.Voxels[voxelType].RightAtlasCoordinate * atlasStep;

                        AddIndices();
                        meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, rightAtlasPosition.x + 0 * atlasStep + uvOffset, rightAtlasPosition.y + 0 * atlasStep + uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, rightAtlasPosition.x + 0 * atlasStep + uvOffset, rightAtlasPosition.y + 1 * atlasStep - uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, rightAtlasPosition.x + 1 * atlasStep - uvOffset, rightAtlasPosition.y + 1 * atlasStep - uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, rightAtlasPosition.x + 1 * atlasStep - uvOffset, rightAtlasPosition.y + 0 * atlasStep + uvOffset));

                        if (isSolid)
                        {
                            AddColliderIndices();
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 1));
                        }
                    }

                    // Left face.
                    if (IsVoxelTransparent(x - 1, y, z))
                    {
                        Vector2 leftAtlasPosition = (Vector2)world.Voxels[voxelType].LeftAtlasCoordinate * atlasStep;

                        AddIndices();
                        meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, leftAtlasPosition.x + 0 * atlasStep + uvOffset, leftAtlasPosition.y + 0 * atlasStep + uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, leftAtlasPosition.x + 0 * atlasStep + uvOffset, leftAtlasPosition.y + 1 * atlasStep - uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, leftAtlasPosition.x + 1 * atlasStep - uvOffset, leftAtlasPosition.y + 1 * atlasStep - uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, leftAtlasPosition.x + 1 * atlasStep - uvOffset, leftAtlasPosition.y + 0 * atlasStep + uvOffset));

                        if (isSolid)
                        {
                            AddColliderIndices();
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 0));
                        }
                    }

                    // Top face.
                    if (IsVoxelTransparent(x, y + 1, z))
                    {
                        Vector2 topAtlasPosition = (Vector2)world.Voxels[voxelType].TopAtlasCoordinate * atlasStep;

                        AddIndices();
                        meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, topAtlasPosition.x + 0 * atlasStep + uvOffset, topAtlasPosition.y + 0 * atlasStep + uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, topAtlasPosition.x + 0 * atlasStep + uvOffset, topAtlasPosition.y + 1 * atlasStep - uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, topAtlasPosition.x + 1 * atlasStep - uvOffset, topAtlasPosition.y + 1 * atlasStep - uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, topAtlasPosition.x + 1 * atlasStep - uvOffset, topAtlasPosition.y + 0 * atlasStep + uvOffset));

                        if (isSolid)
                        {
                            AddColliderIndices();
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 0));
                        }
                    }

                    // Bottom face.
                    if (IsVoxelTransparent(x, y - 1, z))
                    {
                        Vector2 bottomAtlasPosition = (Vector2)world.Voxels[voxelType].BottomAtlasCoordinate * atlasStep;

                        AddIndices();
                        meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, bottomAtlasPosition.x + 0 * atlasStep + uvOffset, bottomAtlasPosition.y + 0 * atlasStep + uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, bottomAtlasPosition.x + 0 * atlasStep + uvOffset, bottomAtlasPosition.y + 1 * atlasStep - uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, bottomAtlasPosition.x + 1 * atlasStep - uvOffset, bottomAtlasPosition.y + 1 * atlasStep - uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, bottomAtlasPosition.x + 1 * atlasStep - uvOffset, bottomAtlasPosition.y + 0 * atlasStep + uvOffset));

                        if (isSolid)
                        {
                            AddColliderIndices();
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 0));
                        }
                    }

                    // Front face.
                    if (IsVoxelTransparent(x, y, z + 1))
                    {
                        Vector2 backAtlasPosition = (Vector2)world.Voxels[voxelType].BackAtlasCoordinate * atlasStep;

                        AddIndices();
                        meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, backAtlasPosition.x + 0 * atlasStep + uvOffset, backAtlasPosition.y + 0 * atlasStep + uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, backAtlasPosition.x + 0 * atlasStep + uvOffset, backAtlasPosition.y + 1 * atlasStep - uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, backAtlasPosition.x + 1 * atlasStep - uvOffset, backAtlasPosition.y + 1 * atlasStep - uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, backAtlasPosition.x + 1 * atlasStep - uvOffset, backAtlasPosition.y + 0 * atlasStep + uvOffset));

                        if (isSolid)
                        {
                            AddColliderIndices();
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 1));
                        }
                    }

                    // Back face.
                    if (IsVoxelTransparent(x, y, z - 1))
                    {
                        Vector2 frontAtlasPosition = (Vector2)world.Voxels[voxelType].FrontAtlasCoordinate * atlasStep;

                        AddIndices();
                        meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, frontAtlasPosition.x + 0 * atlasStep + uvOffset, frontAtlasPosition.y + 0 * atlasStep + uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, frontAtlasPosition.x + 0 * atlasStep + uvOffset, frontAtlasPosition.y + 1 * atlasStep - uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, frontAtlasPosition.x + 1 * atlasStep - uvOffset, frontAtlasPosition.y + 1 * atlasStep - uvOffset));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, frontAtlasPosition.x + 1 * atlasStep - uvOffset, frontAtlasPosition.y + 0 * atlasStep + uvOffset));

                        if (isSolid)
                        {
                            AddColliderIndices();
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 0));
                        }
                    }
                }
            });

            UpdateMesh(meshDatas);
        }

        public void MarkDirty()
        {
            isDirty = true;
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