using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using System.Linq;

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

        public static Dictionary<MaterialType, MeshData> GenerateMeshData(World world, ChunkData chunkData)
        {
            VoxelType GetVoxel(int x, int y, int z)
            {
                Vector3Int globalVoxelCoordinate = CoordinateUtility.ToGlobal(chunkData.Coordinate, new Vector3Int(x, y, z));
                return world.GetVoxel(globalVoxelCoordinate);
            }

            int GetLight(int x, int y, int z, LightMap.Chanel chanel)
            {
                return world.GetLight(CoordinateUtility.ToGlobal(chunkData.Coordinate, new Vector3Int(x, y, z)), chanel);
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
                VoxelType voxelType = chunkData.VoxelMap[x, y, z];
                if (voxelType != VoxelType.Air)
                {
                    MaterialType materialType = world.Voxels[voxelType].MaterialType;
                    if (!meshDatas.ContainsKey(materialType))
                        meshDatas.TryAdd(materialType, new MeshData());

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

                        bool t0 = !IsVoxelTransparent(x + 1, y + 0, z + 1);
                        bool t90 = !IsVoxelTransparent(x + 1, y + 1, z + 0);
                        bool t180 = !IsVoxelTransparent(x + 1, y + 0, z - 1);
                        bool t270 = !IsVoxelTransparent(x + 1, y - 1, z + 0);

                        int lr = GetLight(x + 1, y + 0, z + 0, LightMap.Chanel.Red);
                        int lr0 = GetLight(x + 1, y + 0, z + 1, LightMap.Chanel.Red);
                        int lr45 = GetLight(x + 1, y + 1, z + 1, LightMap.Chanel.Red);
                        int lr90 = GetLight(x + 1, y + 1, z + 0, LightMap.Chanel.Red);
                        int lr135 = GetLight(x + 1, y + 1, z - 1, LightMap.Chanel.Red);
                        int lr180 = GetLight(x + 1, y + 0, z - 1, LightMap.Chanel.Red);
                        int lr225 = GetLight(x + 1, y - 1, z - 1, LightMap.Chanel.Red);
                        int lr270 = GetLight(x + 1, y - 1, z + 0, LightMap.Chanel.Red);
                        int lr315 = GetLight(x + 1, y - 1, z + 1, LightMap.Chanel.Red);

                        float lr1 = (t180 && t270 ? lr : lr + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                        float lr2 = (t90 && t180 ? lr : lr + lr90 + lr135 + lr180) / 4.0f / 15.0f;
                        float lr3 = (t0 && t90 ? lr : lr + lr0 + lr45 + lr90) / 4.0f / 15.0f;
                        float lr4 = (t0 && t270 ? lr : lr + lr0 + lr270 + lr315) / 4.0f / 15.0f;

                        int lg = GetLight(x + 1, y + 0, z + 0, LightMap.Chanel.Green);
                        int lg0 = GetLight(x + 1, y + 0, z + 1, LightMap.Chanel.Green);
                        int lg45 = GetLight(x + 1, y + 1, z + 1, LightMap.Chanel.Green);
                        int lg90 = GetLight(x + 1, y + 1, z + 0, LightMap.Chanel.Green);
                        int lg135 = GetLight(x + 1, y + 1, z - 1, LightMap.Chanel.Green);
                        int lg180 = GetLight(x + 1, y + 0, z - 1, LightMap.Chanel.Green);
                        int lg225 = GetLight(x + 1, y - 1, z - 1, LightMap.Chanel.Green);
                        int lg270 = GetLight(x + 1, y - 1, z + 0, LightMap.Chanel.Green);
                        int lg315 = GetLight(x + 1, y - 1, z + 1, LightMap.Chanel.Green);

                        float lg1 = (t180 && t270 ? lg : lg + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                        float lg2 = (t90 && t180 ? lg : lg + lg90 + lg135 + lg180) / 4.0f / 15.0f;
                        float lg3 = (t0 && t90 ? lg : lg + lg0 + lg45 + lg90) / 4.0f / 15.0f;
                        float lg4 = (t0 && t270 ? lg : lg + lg0 + lg270 + lg315) / 4.0f / 15.0f;

                        int lb = GetLight(x + 1, y + 0, z + 0, LightMap.Chanel.Blue);
                        int lb0 = GetLight(x + 1, y + 0, z + 1, LightMap.Chanel.Blue);
                        int lb45 = GetLight(x + 1, y + 1, z + 1, LightMap.Chanel.Blue);
                        int lb90 = GetLight(x + 1, y + 1, z + 0, LightMap.Chanel.Blue);
                        int lb135 = GetLight(x + 1, y + 1, z - 1, LightMap.Chanel.Blue);
                        int lb180 = GetLight(x + 1, y + 0, z - 1, LightMap.Chanel.Blue);
                        int lb225 = GetLight(x + 1, y - 1, z - 1, LightMap.Chanel.Blue);
                        int lb270 = GetLight(x + 1, y - 1, z + 0, LightMap.Chanel.Blue);
                        int lb315 = GetLight(x + 1, y - 1, z + 1, LightMap.Chanel.Blue);

                        float lb1 = (t180 && t270 ? lb : lb + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                        float lb2 = (t90 && t180 ? lb : lb + lb90 + lb135 + lb180) / 4.0f / 15.0f;
                        float lb3 = (t0 && t90 ? lb : lb + lb0 + lb45 + lb90) / 4.0f / 15.0f;
                        float lb4 = (t0 && t270 ? lb : lb + lb0 + lb270 + lb315) / 4.0f / 15.0f;

                        int ls = GetLight(x + 1, y + 0, z + 0, LightMap.Chanel.Sun);
                        int ls0 = GetLight(x + 1, y + 0, z + 1, LightMap.Chanel.Sun);
                        int ls45 = GetLight(x + 1, y + 1, z + 1, LightMap.Chanel.Sun);
                        int ls90 = GetLight(x + 1, y + 1, z + 0, LightMap.Chanel.Sun);
                        int ls135 = GetLight(x + 1, y + 1, z - 1, LightMap.Chanel.Sun);
                        int ls180 = GetLight(x + 1, y + 0, z - 1, LightMap.Chanel.Sun);
                        int ls225 = GetLight(x + 1, y - 1, z - 1, LightMap.Chanel.Sun);
                        int ls270 = GetLight(x + 1, y - 1, z + 0, LightMap.Chanel.Sun);
                        int ls315 = GetLight(x + 1, y - 1, z + 1, LightMap.Chanel.Sun);

                        float ls1 = (t180 && t270 ? ls : ls + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                        float ls2 = (t90 && t180 ? ls : ls + ls90 + ls135 + ls180) / 4.0f / 15.0f;
                        float ls3 = (t0 && t90 ? ls : ls + ls0 + ls45 + ls90) / 4.0f / 15.0f;
                        float ls4 = (t0 && t270 ? ls : ls + ls0 + ls270 + ls315) / 4.0f / 15.0f;

                        AddIndices();
                        meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, rightAtlasPosition.x + 0 * atlasStep + uvOffset, rightAtlasPosition.y + 0 * atlasStep + uvOffset, lr1, lg1, lb1, ls1));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, rightAtlasPosition.x + 0 * atlasStep + uvOffset, rightAtlasPosition.y + 1 * atlasStep - uvOffset, lr2, lg2, lb2, ls2));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, rightAtlasPosition.x + 1 * atlasStep - uvOffset, rightAtlasPosition.y + 1 * atlasStep - uvOffset, lr3, lg3, lb3, ls3));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, rightAtlasPosition.x + 1 * atlasStep - uvOffset, rightAtlasPosition.y + 0 * atlasStep + uvOffset, lr4, lg4, lb4, ls4));

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

                        bool t0 = !IsVoxelTransparent(x - 1, y + 0, z - 1);
                        bool t90 = !IsVoxelTransparent(x - 1, y + 1, z + 0);
                        bool t180 = !IsVoxelTransparent(x - 1, y + 0, z + 1);
                        bool t270 = !IsVoxelTransparent(x - 1, y - 1, z + 0);

                        int lr = GetLight(x - 1, y + 0, z + 0, LightMap.Chanel.Red);
                        int lr0 = GetLight(x - 1, y + 0, z - 1, LightMap.Chanel.Red);
                        int lr45 = GetLight(x - 1, y + 1, z - 1, LightMap.Chanel.Red);
                        int lr90 = GetLight(x - 1, y + 1, z + 0, LightMap.Chanel.Red);
                        int lr135 = GetLight(x - 1, y + 1, z + 1, LightMap.Chanel.Red);
                        int lr180 = GetLight(x - 1, y + 0, z + 1, LightMap.Chanel.Red);
                        int lr225 = GetLight(x - 1, y - 1, z + 1, LightMap.Chanel.Red);
                        int lr270 = GetLight(x - 1, y - 1, z + 0, LightMap.Chanel.Red);
                        int lr315 = GetLight(x - 1, y - 1, z - 1, LightMap.Chanel.Red);

                        float lr1 = (t180 && t270 ? lr : lr + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                        float lr2 = (t90 && t180 ? lr : lr + lr90 + lr135 + lr180) / 4.0f / 15.0f;
                        float lr3 = (t0 && t90 ? lr : lr + lr0 + lr45 + lr90) / 4.0f / 15.0f;
                        float lr4 = (t0 && t270 ? lr : lr + lr0 + lr270 + lr315) / 4.0f / 15.0f;

                        int lg = GetLight(x - 1, y + 0, z + 0, LightMap.Chanel.Green);
                        int lg0 = GetLight(x - 1, y + 0, z - 1, LightMap.Chanel.Green);
                        int lg45 = GetLight(x - 1, y + 1, z - 1, LightMap.Chanel.Green);
                        int lg90 = GetLight(x - 1, y + 1, z + 0, LightMap.Chanel.Green);
                        int lg135 = GetLight(x - 1, y + 1, z + 1, LightMap.Chanel.Green);
                        int lg180 = GetLight(x - 1, y + 0, z + 1, LightMap.Chanel.Green);
                        int lg225 = GetLight(x - 1, y - 1, z + 1, LightMap.Chanel.Green);
                        int lg270 = GetLight(x - 1, y - 1, z + 0, LightMap.Chanel.Green);
                        int lg315 = GetLight(x - 1, y - 1, z - 1, LightMap.Chanel.Green);

                        float lg1 = (t180 && t270 ? lg : lg + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                        float lg2 = (t90 && t180 ? lg : lg + lg90 + lg135 + lg180) / 4.0f / 15.0f;
                        float lg3 = (t0 && t90 ? lg : lg + lg0 + lg45 + lg90) / 4.0f / 15.0f;
                        float lg4 = (t0 && t270 ? lg : lg + lg0 + lg270 + lg315) / 4.0f / 15.0f;

                        int lb = GetLight(x - 1, y + 0, z + 0, LightMap.Chanel.Blue);
                        int lb0 = GetLight(x - 1, y + 0, z - 1, LightMap.Chanel.Blue);
                        int lb45 = GetLight(x - 1, y + 1, z - 1, LightMap.Chanel.Blue);
                        int lb90 = GetLight(x - 1, y + 1, z + 0, LightMap.Chanel.Blue);
                        int lb135 = GetLight(x - 1, y + 1, z + 1, LightMap.Chanel.Blue);
                        int lb180 = GetLight(x - 1, y + 0, z + 1, LightMap.Chanel.Blue);
                        int lb225 = GetLight(x - 1, y - 1, z + 1, LightMap.Chanel.Blue);
                        int lb270 = GetLight(x - 1, y - 1, z + 0, LightMap.Chanel.Blue);
                        int lb315 = GetLight(x - 1, y - 1, z - 1, LightMap.Chanel.Blue);

                        float lb1 = (t180 && t270 ? lb : lb + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                        float lb2 = (t90 && t180 ? lb : lb + lb90 + lb135 + lb180) / 4.0f / 15.0f;
                        float lb3 = (t0 && t90 ? lb : lb + lb0 + lb45 + lb90) / 4.0f / 15.0f;
                        float lb4 = (t0 && t270 ? lb : lb + lb0 + lb270 + lb315) / 4.0f / 15.0f;

                        int ls = GetLight(x - 1, y + 0, z + 0, LightMap.Chanel.Sun);
                        int ls0 = GetLight(x - 1, y + 0, z - 1, LightMap.Chanel.Sun);
                        int ls45 = GetLight(x - 1, y + 1, z - 1, LightMap.Chanel.Sun);
                        int ls90 = GetLight(x - 1, y + 1, z + 0, LightMap.Chanel.Sun);
                        int ls135 = GetLight(x - 1, y + 1, z + 1, LightMap.Chanel.Sun);
                        int ls180 = GetLight(x - 1, y + 0, z + 1, LightMap.Chanel.Sun);
                        int ls225 = GetLight(x - 1, y - 1, z + 1, LightMap.Chanel.Sun);
                        int ls270 = GetLight(x - 1, y - 1, z + 0, LightMap.Chanel.Sun);
                        int ls315 = GetLight(x - 1, y - 1, z - 1, LightMap.Chanel.Sun);

                        float ls1 = (t180 && t270 ? ls : ls + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                        float ls2 = (t90 && t180 ? ls : ls + ls90 + ls135 + ls180) / 4.0f / 15.0f;
                        float ls3 = (t0 && t90 ? ls : ls + ls0 + ls45 + ls90) / 4.0f / 15.0f;
                        float ls4 = (t0 && t270 ? ls : ls + ls0 + ls270 + ls315) / 4.0f / 15.0f;

                        AddIndices();
                        meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, leftAtlasPosition.x + 0 * atlasStep + uvOffset, leftAtlasPosition.y + 0 * atlasStep + uvOffset, lr1, lg1, lb1, ls1));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, leftAtlasPosition.x + 0 * atlasStep + uvOffset, leftAtlasPosition.y + 1 * atlasStep - uvOffset, lr2, lg2, lb2, ls2));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, leftAtlasPosition.x + 1 * atlasStep - uvOffset, leftAtlasPosition.y + 1 * atlasStep - uvOffset, lr3, lg3, lb3, ls3));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, leftAtlasPosition.x + 1 * atlasStep - uvOffset, leftAtlasPosition.y + 0 * atlasStep + uvOffset, lr4, lg4, lb4, ls4));

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

                        bool t0 = !IsVoxelTransparent(x + 1, y + 1, z + 0);
                        bool t90 = !IsVoxelTransparent(x + 0, y + 1, z + 1);
                        bool t180 = !IsVoxelTransparent(x - 1, y + 1, z + 0);
                        bool t270 = !IsVoxelTransparent(x + 0, y + 1, z - 1);

                        int lr = GetLight(x + 0, y + 1, z + 0, LightMap.Chanel.Red);
                        int lr0 = GetLight(x + 1, y + 1, z + 0, LightMap.Chanel.Red);
                        int lr45 = GetLight(x + 1, y + 1, z + 1, LightMap.Chanel.Red);
                        int lr90 = GetLight(x + 0, y + 1, z + 1, LightMap.Chanel.Red);
                        int lr135 = GetLight(x - 1, y + 1, z + 1, LightMap.Chanel.Red);
                        int lr180 = GetLight(x - 1, y + 1, z + 0, LightMap.Chanel.Red);
                        int lr225 = GetLight(x - 1, y + 1, z - 1, LightMap.Chanel.Red);
                        int lr270 = GetLight(x + 0, y + 1, z - 1, LightMap.Chanel.Red);
                        int lr315 = GetLight(x + 1, y + 1, z - 1, LightMap.Chanel.Red);

                        float lr1 = (t180 && t270 ? lr : lr + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                        float lr2 = (t90 && t180 ? lr : lr + lr90 + lr135 + lr180) / 4.0f / 15.0f;
                        float lr3 = (t0 && t90 ? lr : lr + lr0 + lr45 + lr90) / 4.0f / 15.0f;
                        float lr4 = (t0 && t270 ? lr : lr + lr0 + lr270 + lr315) / 4.0f / 15.0f;

                        int lg = GetLight(x + 0, y + 1, z + 0, LightMap.Chanel.Green);
                        int lg0 = GetLight(x + 1, y + 1, z + 0, LightMap.Chanel.Green);
                        int lg45 = GetLight(x + 1, y + 1, z + 1, LightMap.Chanel.Green);
                        int lg90 = GetLight(x + 0, y + 1, z + 1, LightMap.Chanel.Green);
                        int lg135 = GetLight(x - 1, y + 1, z + 1, LightMap.Chanel.Green);
                        int lg180 = GetLight(x - 1, y + 1, z + 0, LightMap.Chanel.Green);
                        int lg225 = GetLight(x - 1, y + 1, z - 1, LightMap.Chanel.Green);
                        int lg270 = GetLight(x + 0, y + 1, z - 1, LightMap.Chanel.Green);
                        int lg315 = GetLight(x + 1, y + 1, z - 1, LightMap.Chanel.Green);

                        float lg1 = (t180 && t270 ? lg : lg + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                        float lg2 = (t90 && t180 ? lg : lg + lg90 + lg135 + lg180) / 4.0f / 15.0f;
                        float lg3 = (t0 && t90 ? lg : lg + lg0 + lg45 + lg90) / 4.0f / 15.0f;
                        float lg4 = (t0 && t270 ? lg : lg + lg0 + lg270 + lg315) / 4.0f / 15.0f;

                        int lb = GetLight(x + 0, y + 1, z + 0, LightMap.Chanel.Blue);
                        int lb0 = GetLight(x + 1, y + 1, z + 0, LightMap.Chanel.Blue);
                        int lb45 = GetLight(x + 1, y + 1, z + 1, LightMap.Chanel.Blue);
                        int lb90 = GetLight(x + 0, y + 1, z + 1, LightMap.Chanel.Blue);
                        int lb135 = GetLight(x - 1, y + 1, z + 1, LightMap.Chanel.Blue);
                        int lb180 = GetLight(x - 1, y + 1, z + 0, LightMap.Chanel.Blue);
                        int lb225 = GetLight(x - 1, y + 1, z - 1, LightMap.Chanel.Blue);
                        int lb270 = GetLight(x + 0, y + 1, z - 1, LightMap.Chanel.Blue);
                        int lb315 = GetLight(x + 1, y + 1, z - 1, LightMap.Chanel.Blue);

                        float lb1 = (t180 && t270 ? lb : lb + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                        float lb2 = (t90 && t180 ? lb : lb + lb90 + lb135 + lb180) / 4.0f / 15.0f;
                        float lb3 = (t0 && t90 ? lb : lb + lb0 + lb45 + lb90) / 4.0f / 15.0f;
                        float lb4 = (t0 && t270 ? lb : lb + lb0 + lb270 + lb315) / 4.0f / 15.0f;

                        int ls = GetLight(x + 0, y + 1, z + 0, LightMap.Chanel.Sun);
                        int ls0 = GetLight(x + 1, y + 1, z + 0, LightMap.Chanel.Sun);
                        int ls45 = GetLight(x + 1, y + 1, z + 1, LightMap.Chanel.Sun);
                        int ls90 = GetLight(x + 0, y + 1, z + 1, LightMap.Chanel.Sun);
                        int ls135 = GetLight(x - 1, y + 1, z + 1, LightMap.Chanel.Sun);
                        int ls180 = GetLight(x - 1, y + 1, z + 0, LightMap.Chanel.Sun);
                        int ls225 = GetLight(x - 1, y + 1, z - 1, LightMap.Chanel.Sun);
                        int ls270 = GetLight(x + 0, y + 1, z - 1, LightMap.Chanel.Sun);
                        int ls315 = GetLight(x + 1, y + 1, z - 1, LightMap.Chanel.Sun);

                        float ls1 = (t180 && t270 ? ls : ls + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                        float ls2 = (t90 && t180 ? ls : ls + ls90 + ls135 + ls180) / 4.0f / 15.0f;
                        float ls3 = (t0 && t90 ? ls : ls + ls0 + ls45 + ls90) / 4.0f / 15.0f;
                        float ls4 = (t0 && t270 ? ls : ls + ls0 + ls270 + ls315) / 4.0f / 15.0f;

                        AddIndices();
                        meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, topAtlasPosition.x + 0 * atlasStep + uvOffset, topAtlasPosition.y + 0 * atlasStep + uvOffset, lr1, lg1, lb1, ls1));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, topAtlasPosition.x + 0 * atlasStep + uvOffset, topAtlasPosition.y + 1 * atlasStep - uvOffset, lr2, lg2, lb2, ls2));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, topAtlasPosition.x + 1 * atlasStep - uvOffset, topAtlasPosition.y + 1 * atlasStep - uvOffset, lr3, lg3, lb3, ls3));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, topAtlasPosition.x + 1 * atlasStep - uvOffset, topAtlasPosition.y + 0 * atlasStep + uvOffset, lr4, lg4, lb4, ls4));

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

                        bool t0 = !IsVoxelTransparent(x - 1, y - 1, z + 0);
                        bool t90 = !IsVoxelTransparent(x + 0, y - 1, z + 1);
                        bool t180 = !IsVoxelTransparent(x + 1, y - 1, z + 0);
                        bool t270 = !IsVoxelTransparent(x + 0, y - 1, z - 1);

                        int lr = GetLight(x + 0, y - 1, z + 0, LightMap.Chanel.Red);
                        int lr0 = GetLight(x - 1, y - 1, z + 0, LightMap.Chanel.Red);
                        int lr45 = GetLight(x - 1, y - 1, z + 1, LightMap.Chanel.Red);
                        int lr90 = GetLight(x + 0, y - 1, z + 1, LightMap.Chanel.Red);
                        int lr135 = GetLight(x + 1, y - 1, z + 1, LightMap.Chanel.Red);
                        int lr180 = GetLight(x + 1, y - 1, z + 0, LightMap.Chanel.Red);
                        int lr225 = GetLight(x + 1, y - 1, z - 1, LightMap.Chanel.Red);
                        int lr270 = GetLight(x + 0, y - 1, z - 1, LightMap.Chanel.Red);
                        int lr315 = GetLight(x - 1, y - 1, z - 1, LightMap.Chanel.Red);

                        float lr1 = (t180 && t270 ? lr : lr + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                        float lr2 = (t90 && t180 ? lr : lr + lr90 + lr135 + lr180) / 4.0f / 15.0f;
                        float lr3 = (t0 && t90 ? lr : lr + lr0 + lr45 + lr90) / 4.0f / 15.0f;
                        float lr4 = (t0 && t270 ? lr : lr + lr0 + lr270 + lr315) / 4.0f / 15.0f;

                        int lg = GetLight(x + 0, y - 1, z + 0, LightMap.Chanel.Green);
                        int lg0 = GetLight(x - 1, y - 1, z + 0, LightMap.Chanel.Green);
                        int lg45 = GetLight(x - 1, y - 1, z + 1, LightMap.Chanel.Green);
                        int lg90 = GetLight(x + 0, y - 1, z + 1, LightMap.Chanel.Green);
                        int lg135 = GetLight(x + 1, y - 1, z + 1, LightMap.Chanel.Green);
                        int lg180 = GetLight(x + 1, y - 1, z + 0, LightMap.Chanel.Green);
                        int lg225 = GetLight(x + 1, y - 1, z - 1, LightMap.Chanel.Green);
                        int lg270 = GetLight(x + 0, y - 1, z - 1, LightMap.Chanel.Green);
                        int lg315 = GetLight(x - 1, y - 1, z - 1, LightMap.Chanel.Green);

                        float lg1 = (t180 && t270 ? lg : lg + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                        float lg2 = (t90 && t180 ? lg : lg + lg90 + lg135 + lg180) / 4.0f / 15.0f;
                        float lg3 = (t0 && t90 ? lg : lg + lg0 + lg45 + lg90) / 4.0f / 15.0f;
                        float lg4 = (t0 && t270 ? lg : lg + lg0 + lg270 + lg315) / 4.0f / 15.0f;

                        int lb = GetLight(x + 0, y - 1, z + 0, LightMap.Chanel.Blue);
                        int lb0 = GetLight(x - 1, y - 1, z + 0, LightMap.Chanel.Blue);
                        int lb45 = GetLight(x - 1, y - 1, z + 1, LightMap.Chanel.Blue);
                        int lb90 = GetLight(x + 0, y - 1, z + 1, LightMap.Chanel.Blue);
                        int lb135 = GetLight(x + 1, y - 1, z + 1, LightMap.Chanel.Blue);
                        int lb180 = GetLight(x + 1, y - 1, z + 0, LightMap.Chanel.Blue);
                        int lb225 = GetLight(x + 1, y - 1, z - 1, LightMap.Chanel.Blue);
                        int lb270 = GetLight(x + 0, y - 1, z - 1, LightMap.Chanel.Blue);
                        int lb315 = GetLight(x - 1, y - 1, z - 1, LightMap.Chanel.Blue);

                        float lb1 = (t180 && t270 ? lb : lb + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                        float lb2 = (t90 && t180 ? lb : lb + lb90 + lb135 + lb180) / 4.0f / 15.0f;
                        float lb3 = (t0 && t90 ? lb : lb + lb0 + lb45 + lb90) / 4.0f / 15.0f;
                        float lb4 = (t0 && t270 ? lb : lb + lb0 + lb270 + lb315) / 4.0f / 15.0f;

                        int ls = GetLight(x + 0, y - 1, z + 0, LightMap.Chanel.Sun);
                        int ls0 = GetLight(x - 1, y - 1, z + 0, LightMap.Chanel.Sun);
                        int ls45 = GetLight(x - 1, y - 1, z + 1, LightMap.Chanel.Sun);
                        int ls90 = GetLight(x + 0, y - 1, z + 1, LightMap.Chanel.Sun);
                        int ls135 = GetLight(x + 1, y - 1, z + 1, LightMap.Chanel.Sun);
                        int ls180 = GetLight(x + 1, y - 1, z + 0, LightMap.Chanel.Sun);
                        int ls225 = GetLight(x + 1, y - 1, z - 1, LightMap.Chanel.Sun);
                        int ls270 = GetLight(x + 0, y - 1, z - 1, LightMap.Chanel.Sun);
                        int ls315 = GetLight(x - 1, y - 1, z - 1, LightMap.Chanel.Sun);

                        float ls1 = (t180 && t270 ? ls : ls + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                        float ls2 = (t90 && t180 ? ls : ls + ls90 + ls135 + ls180) / 4.0f / 15.0f;
                        float ls3 = (t0 && t90 ? ls : ls + ls0 + ls45 + ls90) / 4.0f / 15.0f;
                        float ls4 = (t0 && t270 ? ls : ls + ls0 + ls270 + ls315) / 4.0f / 15.0f;

                        AddIndices();
                        meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, bottomAtlasPosition.x + 0 * atlasStep + uvOffset, bottomAtlasPosition.y + 0 * atlasStep + uvOffset, lr1, lg1, lb1, ls1));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, bottomAtlasPosition.x + 0 * atlasStep + uvOffset, bottomAtlasPosition.y + 1 * atlasStep - uvOffset, lr2, lg2, lb2, ls2));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, bottomAtlasPosition.x + 1 * atlasStep - uvOffset, bottomAtlasPosition.y + 1 * atlasStep - uvOffset, lr3, lg3, lb3, ls3));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, bottomAtlasPosition.x + 1 * atlasStep - uvOffset, bottomAtlasPosition.y + 0 * atlasStep + uvOffset, lr4, lg4, lb4, ls4));

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

                        bool t0 = !IsVoxelTransparent(x - 1, y + 0, z + 1);
                        bool t90 = !IsVoxelTransparent(x + 0, y + 1, z + 1);
                        bool t180 = !IsVoxelTransparent(x + 1, y + 0, z + 1);
                        bool t270 = !IsVoxelTransparent(x + 0, y - 1, z + 1);

                        int lr = GetLight(x + 0, y + 0, z + 1, LightMap.Chanel.Red);
                        int lr0 = GetLight(x - 1, y + 0, z + 1, LightMap.Chanel.Red);
                        int lr45 = GetLight(x - 1, y + 1, z + 1, LightMap.Chanel.Red);
                        int lr90 = GetLight(x + 0, y + 1, z + 1, LightMap.Chanel.Red);
                        int lr135 = GetLight(x + 1, y + 1, z + 1, LightMap.Chanel.Red);
                        int lr180 = GetLight(x + 1, y + 0, z + 1, LightMap.Chanel.Red);
                        int lr225 = GetLight(x + 1, y - 1, z + 1, LightMap.Chanel.Red);
                        int lr270 = GetLight(x + 0, y - 1, z + 1, LightMap.Chanel.Red);
                        int lr315 = GetLight(x - 1, y - 1, z + 1, LightMap.Chanel.Red);

                        float lr1 = (t180 && t270 ? lr : lr + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                        float lr2 = (t90 && t180 ? lr : lr + lr90 + lr135 + lr180) / 4.0f / 15.0f;
                        float lr3 = (t0 && t90 ? lr : lr + lr0 + lr45 + lr90) / 4.0f / 15.0f;
                        float lr4 = (t0 && t270 ? lr : lr + lr0 + lr270 + lr315) / 4.0f / 15.0f;

                        int lg = GetLight(x + 0, y + 0, z + 1, LightMap.Chanel.Green);
                        int lg0 = GetLight(x - 1, y + 0, z + 1, LightMap.Chanel.Green);
                        int lg45 = GetLight(x - 1, y + 1, z + 1, LightMap.Chanel.Green);
                        int lg90 = GetLight(x + 0, y + 1, z + 1, LightMap.Chanel.Green);
                        int lg135 = GetLight(x + 1, y + 1, z + 1, LightMap.Chanel.Green);
                        int lg180 = GetLight(x + 1, y + 0, z + 1, LightMap.Chanel.Green);
                        int lg225 = GetLight(x + 1, y - 1, z + 1, LightMap.Chanel.Green);
                        int lg270 = GetLight(x + 0, y - 1, z + 1, LightMap.Chanel.Green);
                        int lg315 = GetLight(x - 1, y - 1, z + 1, LightMap.Chanel.Green);

                        float lg1 = (t180 && t270 ? lg : lg + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                        float lg2 = (t90 && t180 ? lg : lg + lg90 + lg135 + lg180) / 4.0f / 15.0f;
                        float lg3 = (t0 && t90 ? lg : lg + lg0 + lg45 + lg90) / 4.0f / 15.0f;
                        float lg4 = (t0 && t270 ? lg : lg + lg0 + lg270 + lg315) / 4.0f / 15.0f;

                        int lb = GetLight(x + 0, y + 0, z + 1, LightMap.Chanel.Blue);
                        int lb0 = GetLight(x - 1, y + 0, z + 1, LightMap.Chanel.Blue);
                        int lb45 = GetLight(x - 1, y + 1, z + 1, LightMap.Chanel.Blue);
                        int lb90 = GetLight(x + 0, y + 1, z + 1, LightMap.Chanel.Blue);
                        int lb135 = GetLight(x + 1, y + 1, z + 1, LightMap.Chanel.Blue);
                        int lb180 = GetLight(x + 1, y + 0, z + 1, LightMap.Chanel.Blue);
                        int lb225 = GetLight(x + 1, y - 1, z + 1, LightMap.Chanel.Blue);
                        int lb270 = GetLight(x + 0, y - 1, z + 1, LightMap.Chanel.Blue);
                        int lb315 = GetLight(x - 1, y - 1, z + 1, LightMap.Chanel.Blue);

                        float lb1 = (t180 && t270 ? lb : lb + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                        float lb2 = (t90 && t180 ? lb : lb + lb90 + lb135 + lb180) / 4.0f / 15.0f;
                        float lb3 = (t0 && t90 ? lb : lb + lb0 + lb45 + lb90) / 4.0f / 15.0f;
                        float lb4 = (t0 && t270 ? lb : lb + lb0 + lb270 + lb315) / 4.0f / 15.0f;

                        int ls = GetLight(x + 0, y + 0, z + 1, LightMap.Chanel.Sun);
                        int ls0 = GetLight(x - 1, y + 0, z + 1, LightMap.Chanel.Sun);
                        int ls45 = GetLight(x - 1, y + 1, z + 1, LightMap.Chanel.Sun);
                        int ls90 = GetLight(x + 0, y + 1, z + 1, LightMap.Chanel.Sun);
                        int ls135 = GetLight(x + 1, y + 1, z + 1, LightMap.Chanel.Sun);
                        int ls180 = GetLight(x + 1, y + 0, z + 1, LightMap.Chanel.Sun);
                        int ls225 = GetLight(x + 1, y - 1, z + 1, LightMap.Chanel.Sun);
                        int ls270 = GetLight(x + 0, y - 1, z + 1, LightMap.Chanel.Sun);
                        int ls315 = GetLight(x - 1, y - 1, z + 1, LightMap.Chanel.Sun);

                        float ls1 = (t180 && t270 ? ls : ls + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                        float ls2 = (t90 && t180 ? ls : ls + ls90 + ls135 + ls180) / 4.0f / 15.0f;
                        float ls3 = (t0 && t90 ? ls : ls + ls0 + ls45 + ls90) / 4.0f / 15.0f;
                        float ls4 = (t0 && t270 ? ls : ls + ls0 + ls270 + ls315) / 4.0f / 15.0f;

                        AddIndices();
                        meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, backAtlasPosition.x + 0 * atlasStep + uvOffset, backAtlasPosition.y + 0 * atlasStep + uvOffset, lr1, lg1, lb1, ls1));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, backAtlasPosition.x + 0 * atlasStep + uvOffset, backAtlasPosition.y + 1 * atlasStep - uvOffset, lr2, lg2, lb2, ls2));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, backAtlasPosition.x + 1 * atlasStep - uvOffset, backAtlasPosition.y + 1 * atlasStep - uvOffset, lr3, lg3, lb3, ls3));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, backAtlasPosition.x + 1 * atlasStep - uvOffset, backAtlasPosition.y + 0 * atlasStep + uvOffset, lr4, lg4, lb4, ls4));

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

                        bool t0 = !IsVoxelTransparent(x + 1, y + 0, z - 1);
                        bool t90 = !IsVoxelTransparent(x + 0, y + 1, z - 1);
                        bool t180 = !IsVoxelTransparent(x - 1, y + 0, z - 1);
                        bool t270 = !IsVoxelTransparent(x + 0, y - 1, z - 1);

                        int lr = GetLight(x + 0, y + 0, z - 1, LightMap.Chanel.Red);
                        int lr0 = GetLight(x + 1, y + 0, z - 1, LightMap.Chanel.Red);
                        int lr45 = GetLight(x + 1, y + 1, z - 1, LightMap.Chanel.Red);
                        int lr90 = GetLight(x + 0, y + 1, z - 1, LightMap.Chanel.Red);
                        int lr135 = GetLight(x - 1, y + 1, z - 1, LightMap.Chanel.Red);
                        int lr180 = GetLight(x - 1, y + 0, z - 1, LightMap.Chanel.Red);
                        int lr225 = GetLight(x - 1, y - 1, z - 1, LightMap.Chanel.Red);
                        int lr270 = GetLight(x + 0, y - 1, z - 1, LightMap.Chanel.Red);
                        int lr315 = GetLight(x + 1, y - 1, z - 1, LightMap.Chanel.Red);

                        float lr1 = (t180 && t270 ? lr : lr + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                        float lr2 = (t90 && t180 ? lr : lr + lr90 + lr135 + lr180) / 4.0f / 15.0f;
                        float lr3 = (t0 && t90 ? lr : lr + lr0 + lr45 + lr90) / 4.0f / 15.0f;
                        float lr4 = (t0 && t270 ? lr : lr + lr0 + lr270 + lr315) / 4.0f / 15.0f;

                        int lg = GetLight(x + 0, y + 0, z - 1, LightMap.Chanel.Green);
                        int lg0 = GetLight(x + 1, y + 0, z - 1, LightMap.Chanel.Green);
                        int lg45 = GetLight(x + 1, y + 1, z - 1, LightMap.Chanel.Green);
                        int lg90 = GetLight(x + 0, y + 1, z - 1, LightMap.Chanel.Green);
                        int lg135 = GetLight(x - 1, y + 1, z - 1, LightMap.Chanel.Green);
                        int lg180 = GetLight(x - 1, y + 0, z - 1, LightMap.Chanel.Green);
                        int lg225 = GetLight(x - 1, y - 1, z - 1, LightMap.Chanel.Green);
                        int lg270 = GetLight(x + 0, y - 1, z - 1, LightMap.Chanel.Green);
                        int lg315 = GetLight(x + 1, y - 1, z - 1, LightMap.Chanel.Green);

                        float lg1 = (t180 && t270 ? lg : lg + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                        float lg2 = (t90 && t180 ? lg : lg + lg90 + lg135 + lg180) / 4.0f / 15.0f;
                        float lg3 = (t0 && t90 ? lg : lg + lg0 + lg45 + lg90) / 4.0f / 15.0f;
                        float lg4 = (t0 && t270 ? lg : lg + lg0 + lg270 + lg315) / 4.0f / 15.0f;

                        int lb = GetLight(x + 0, y + 0, z - 1, LightMap.Chanel.Blue);
                        int lb0 = GetLight(x + 1, y + 0, z - 1, LightMap.Chanel.Blue);
                        int lb45 = GetLight(x + 1, y + 1, z - 1, LightMap.Chanel.Blue);
                        int lb90 = GetLight(x + 0, y + 1, z - 1, LightMap.Chanel.Blue);
                        int lb135 = GetLight(x - 1, y + 1, z - 1, LightMap.Chanel.Blue);
                        int lb180 = GetLight(x - 1, y + 0, z - 1, LightMap.Chanel.Blue);
                        int lb225 = GetLight(x - 1, y - 1, z - 1, LightMap.Chanel.Blue);
                        int lb270 = GetLight(x + 0, y - 1, z - 1, LightMap.Chanel.Blue);
                        int lb315 = GetLight(x + 1, y - 1, z - 1, LightMap.Chanel.Blue);

                        float lb1 = (t180 && t270 ? lb : lb + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                        float lb2 = (t90 && t180 ? lb : lb + lb90 + lb135 + lb180) / 4.0f / 15.0f;
                        float lb3 = (t0 && t90 ? lb : lb + lb0 + lb45 + lb90) / 4.0f / 15.0f;
                        float lb4 = (t0 && t270 ? lb : lb + lb0 + lb270 + lb315) / 4.0f / 15.0f;

                        int ls = GetLight(x + 0, y + 0, z - 1, LightMap.Chanel.Sun);
                        int ls0 = GetLight(x + 1, y + 0, z - 1, LightMap.Chanel.Sun);
                        int ls45 = GetLight(x + 1, y + 1, z - 1, LightMap.Chanel.Sun);
                        int ls90 = GetLight(x + 0, y + 1, z - 1, LightMap.Chanel.Sun);
                        int ls135 = GetLight(x - 1, y + 1, z - 1, LightMap.Chanel.Sun);
                        int ls180 = GetLight(x - 1, y + 0, z - 1, LightMap.Chanel.Sun);
                        int ls225 = GetLight(x - 1, y - 1, z - 1, LightMap.Chanel.Sun);
                        int ls270 = GetLight(x + 0, y - 1, z - 1, LightMap.Chanel.Sun);
                        int ls315 = GetLight(x + 1, y - 1, z - 1, LightMap.Chanel.Sun);

                        float ls1 = (t180 && t270 ? ls : ls + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                        float ls2 = (t90 && t180 ? ls : ls + ls90 + ls135 + ls180) / 4.0f / 15.0f;
                        float ls3 = (t0 && t90 ? ls : ls + ls0 + ls45 + ls90) / 4.0f / 15.0f;
                        float ls4 = (t0 && t270 ? ls : ls + ls0 + ls270 + ls315) / 4.0f / 15.0f;

                        AddIndices();
                        meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, frontAtlasPosition.x + 0 * atlasStep + uvOffset, frontAtlasPosition.y + 0 * atlasStep + uvOffset, lr1, lg1, lb1, ls1));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, frontAtlasPosition.x + 0 * atlasStep + uvOffset, frontAtlasPosition.y + 1 * atlasStep - uvOffset, lr2, lg2, lb2, ls2));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, frontAtlasPosition.x + 1 * atlasStep - uvOffset, frontAtlasPosition.y + 1 * atlasStep - uvOffset, lr3, lg3, lb3, ls3));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, frontAtlasPosition.x + 1 * atlasStep - uvOffset, frontAtlasPosition.y + 0 * atlasStep + uvOffset, lr4, lg4, lb4, ls4));

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