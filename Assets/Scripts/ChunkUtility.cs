using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
    public static class ChunkUtility
    {
        public static void ForEachVoxel(Action<int, int, int> action)
        {
            for (int y = 0; y < Chunk.SIZE; y++)
                for (int x = 0; x < Chunk.SIZE; x++)
                    for (int z = 0; z < Chunk.SIZE; z++)
                        action(y, x, z);
        }

        public static void ForEachVoxel(Action<Vector3Int> action)
        {
            for (int y = 0; y < Chunk.SIZE; y++)
                for (int x = 0; x < Chunk.SIZE; x++)
                    for (int z = 0; z < Chunk.SIZE; z++)
                        action(new Vector3Int(x, y, z));
        }

        public static Dictionary<MaterialType, MeshData> GenerateMeshDatas(World world, Chunk chunk)
        {
            VoxelType GetVoxel(int x, int y, int z)
            {
                Vector3Int globalVoxelCoordinate = CoordinateUtility.ToGlobal(chunk.Coordinate, new Vector3Int(x, y, z));
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

            ForEachVoxel((x, y, z) =>
            {
                VoxelType voxelType = chunk.VoxelMap[x, y, z];
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

            return meshDatas;
        }
    }
}