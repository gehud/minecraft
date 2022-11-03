using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft {
    public static class ChunkUtility {
        public static void ForEachVoxel(Action<int, int, int> action) {
            for (int y = 0; y < Chunk.SIZE; y++)
                for (int x = 0; x < Chunk.SIZE; x++)
                    for (int z = 0; z < Chunk.SIZE; z++)
                        action(y, x, z);
        }

        public static void ForEachVoxel(Action<Vector3Int> action) {
            for (int y = 0; y < Chunk.SIZE; y++)
                for (int x = 0; x < Chunk.SIZE; x++)
                    for (int z = 0; z < Chunk.SIZE; z++)
                        action(new Vector3Int(x, y, z));
        }

        public static Dictionary<MaterialType, MeshData> GenerateMeshData(World world, ChunkData chunkData) {
            var blockDataManager = BlockManager.Instance;

            BlockType GetVoxel(int x, int y, int z) {
                Vector3Int globalVoxelCoordinate = CoordinateUtility.ToGlobal(chunkData.Coordinate, new Vector3Int(x, y, z));
                return world.GetVoxel(globalVoxelCoordinate);
            }

            int GetLight(int x, int y, int z, LightChanel chanel) {
                Vector3Int globalVoxelCoordinate = CoordinateUtility.ToGlobal(chunkData.Coordinate, new Vector3Int(x, y, z));
                return world.GetLight(globalVoxelCoordinate, chanel);
            }

            byte GetLiquidAmount(int x, int y, int z, BlockType liquidType) {
                Vector3Int globalVoxelCoordinate = CoordinateUtility.ToGlobal(chunkData.Coordinate, new Vector3Int(x, y, z));
                return world.GetLiquidAmount(globalVoxelCoordinate, liquidType);
            }

            bool IsVoxelSolid(int x, int y, int z) {
                return blockDataManager.Blocks[GetVoxel(x, y, z)].IsSolid;
            }

            bool IsVoxelTransparent(int x, int y, int z) {
                return blockDataManager.Blocks[GetVoxel(x, y, z)].IsTransparent;
            }

            void AddFaceIndices(MeshData meshData) {
                int vertexCount = meshData.Vertices.Count;
                meshData.Indices.Add((ushort)(0 + vertexCount));
                meshData.Indices.Add((ushort)(1 + vertexCount));
                meshData.Indices.Add((ushort)(2 + vertexCount));
                meshData.Indices.Add((ushort)(0 + vertexCount));
                meshData.Indices.Add((ushort)(2 + vertexCount));
                meshData.Indices.Add((ushort)(3 + vertexCount));
            }

            void AddFaceColliderIndices(MeshData meshData) {
                int vertexCount = meshData.ColliderVertices.Count;
                meshData.ColliderIndices.Add((ushort)(0 + vertexCount));
                meshData.ColliderIndices.Add((ushort)(1 + vertexCount));
                meshData.ColliderIndices.Add((ushort)(2 + vertexCount));
                meshData.ColliderIndices.Add((ushort)(0 + vertexCount));
                meshData.ColliderIndices.Add((ushort)(2 + vertexCount));
                meshData.ColliderIndices.Add((ushort)(3 + vertexCount));
            }

            Dictionary<MaterialType, MeshData> result = new();

            ForEachVoxel((x, y, z) => {
                BlockType voxelType = chunkData.BlockMap[x, y, z];

                if (voxelType != BlockType.Air) {
                    var localVoxelCoordinate = new Vector3Int(x, y, z);
                    MaterialType materialType = blockDataManager.Blocks[voxelType].MaterialType;
                    if (!result.ContainsKey(materialType))
                        result.Add(materialType, new MeshData());

                    float atlasStep = 16.0f / 256.0f;
                    bool isSolid = IsVoxelSolid(x, y, z);
                    var meshData = result[materialType];

                    if (blockDataManager.Blocks[voxelType].IsLiquid) {
                        byte aown = GetLiquidAmount(x + 0, y + 0, z + 0, voxelType);
                        byte atop = GetLiquidAmount(x + 0, y + 1, z + 0, voxelType);
                        byte abot = GetLiquidAmount(x + 0, y - 1, z + 0, voxelType);
                        byte a000 = GetLiquidAmount(x + 1, y + 0, z + 0, voxelType);
                        byte a045 = GetLiquidAmount(x + 1, y + 0, z + 1, voxelType);
                        byte a090 = GetLiquidAmount(x + 0, y + 0, z + 1, voxelType);
                        byte a135 = GetLiquidAmount(x - 1, y + 0, z + 1, voxelType);
                        byte a180 = GetLiquidAmount(x - 1, y + 0, z + 0, voxelType);
                        byte a225 = GetLiquidAmount(x - 1, y + 0, z - 1, voxelType);
                        byte a270 = GetLiquidAmount(x + 0, y + 0, z - 1, voxelType);
                        byte a315 = GetLiquidAmount(x + 1, y + 0, z - 1, voxelType);

                        bool HasFace(int x, int y, int z) {
                            var side = GetLiquidAmount(x, y, z, voxelType);
                            bool isTop = y - localVoxelCoordinate.y == 1;
                            if (isTop)
                                return GetVoxel(x, y, z) != voxelType;
                            return (IsVoxelTransparent(x, y, z) && GetVoxel(x, y, z) != voxelType)
                            || (side != 0 && aown > side && atop != 0);
                        }

                        // Right face.
                        if (HasFace(x + 1, y, z)) {
                            Vector2 atlasPosition = (Vector2)blockDataManager.Blocks[voxelType].RightFace * atlasStep;

                            bool t000 = !IsVoxelTransparent(x + 1, y + 0, z + 1);
                            bool t090 = !IsVoxelTransparent(x + 1, y + 1, z + 0);
                            bool t180 = !IsVoxelTransparent(x + 1, y + 0, z - 1);
                            bool t270 = !IsVoxelTransparent(x + 1, y - 1, z + 0);

                            int lrtop = GetLight(x + 1, y + 0, z + 0, LightChanel.Red);
                            int lr000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Red);
                            int lr045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Red);
                            int lr090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Red);
                            int lr135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Red);
                            int lr180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Red);
                            int lr225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Red);
                            int lr270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Red);
                            int lr315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Red);

                            float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                            float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 15.0f;
                            float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 15.0f;
                            float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 15.0f;

                            int lgtop = GetLight(x + 1, y + 0, z + 0, LightChanel.Green);
                            int lg000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Green);
                            int lg045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Green);
                            int lg090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Green);
                            int lg135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Green);
                            int lg180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Green);
                            int lg225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Green);
                            int lg270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Green);
                            int lg315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Green);

                            float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                            float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 15.0f;
                            float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 15.0f;
                            float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 15.0f;

                            int lbtop = GetLight(x + 1, y + 0, z + 0, LightChanel.Blue);
                            int lb000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Blue);
                            int lb045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Blue);
                            int lb090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Blue);
                            int lb135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Blue);
                            int lb180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Blue);
                            int lb225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Blue);
                            int lb270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Blue);
                            int lb315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Blue);

                            float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                            float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 15.0f;
                            float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 15.0f;
                            float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 15.0f;

                            int lstop = GetLight(x + 1, y + 0, z + 0, LightChanel.Sun);
                            int ls000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Sun);
                            int ls045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Sun);
                            int ls090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Sun);
                            int ls135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Sun);
                            int ls180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Sun);
                            int ls225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Sun);
                            int ls270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Sun);
                            int ls315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Sun);

                            float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                            float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 15.0f;
                            float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 15.0f;
                            float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 15.0f;

                            float h1 = 0.0f;
                            float h2 = 1.0f;
                            float h3 = 1.0f;
                            float h4 = 0.0f;

                            if (atop == 0) {
                                h2 = (aown + a000 + a270 + a315) / 4.0f / LiquidMap.MAX;
                                h3 = (aown + a000 + a045 + a090) / 4.0f / LiquidMap.MAX;
                            }
                            if (a000 != 0) {
                                h1 = (aown + a000 + a270 + a315) / 4.0f / LiquidMap.MAX;
                                h4 = (aown + a000 + a045 + a090) / 4.0f / LiquidMap.MAX;
                            }

                            AddFaceIndices(meshData);
                            meshData.Vertices.Add(new Vertex(x + 1, y + h1, z + 0, atlasPosition.x + 0 * atlasStep, atlasPosition.y + h1 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 1, y + h2, z + 0, atlasPosition.x + 0 * atlasStep, atlasPosition.y + h2 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 1, y + h3, z + 1, atlasPosition.x + 1 * atlasStep, atlasPosition.y + h3 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 1, y + h4, z + 1, atlasPosition.x + 1 * atlasStep, atlasPosition.y + h4 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 1));
                            }
                        }

                        // Left face.
                        if (HasFace(x - 1, y, z)) {
                            Vector2 leftAtlasPosition = (Vector2)blockDataManager.Blocks[voxelType].LeftFace * atlasStep;

                            bool t000 = !IsVoxelTransparent(x - 1, y + 0, z - 1);
                            bool t090 = !IsVoxelTransparent(x - 1, y + 1, z + 0);
                            bool t180 = !IsVoxelTransparent(x - 1, y + 0, z + 1);
                            bool t270 = !IsVoxelTransparent(x - 1, y - 1, z + 0);

                            int lrtop = GetLight(x - 1, y + 0, z + 0, LightChanel.Red);
                            int lr000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Red);
                            int lr045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Red);
                            int lr090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Red);
                            int lr135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Red);
                            int lr180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Red);
                            int lr225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Red);
                            int lr270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Red);
                            int lr315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Red);

                            float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                            float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 15.0f;
                            float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 15.0f;
                            float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 15.0f;

                            int lgtop = GetLight(x - 1, y + 0, z + 0, LightChanel.Green);
                            int lg000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Green);
                            int lg045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Green);
                            int lg090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Green);
                            int lg135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Green);
                            int lg180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Green);
                            int lg225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Green);
                            int lg270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Green);
                            int lg315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Green);

                            float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                            float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 15.0f;
                            float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 15.0f;
                            float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 15.0f;

                            int lbtop = GetLight(x - 1, y + 0, z + 0, LightChanel.Blue);
                            int lb000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Blue);
                            int lb045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Blue);
                            int lb090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Blue);
                            int lb135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Blue);
                            int lb180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Blue);
                            int lb225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Blue);
                            int lb270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Blue);
                            int lb315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Blue);

                            float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                            float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 15.0f;
                            float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 15.0f;
                            float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 15.0f;

                            int lstop = GetLight(x - 1, y + 0, z + 0, LightChanel.Sun);
                            int ls000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Sun);
                            int ls045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Sun);
                            int ls090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Sun);
                            int ls135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Sun);
                            int ls180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Sun);
                            int ls225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Sun);
                            int ls270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Sun);
                            int ls315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Sun);

                            float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                            float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 15.0f;
                            float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 15.0f;
                            float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 15.0f;

                            float h1 = 0.0f;
                            float h2 = 1.0f;
                            float h3 = 1.0f;
                            float h4 = 0.0f;

                            if (atop == 0) {
                                h2 = (aown + a090 + a135 + a180) / 4.0f / LiquidMap.MAX;
                                h3 = (aown + a180 + a225 + a270) / 4.0f / LiquidMap.MAX;
                            }
                            if (a180 != 0) {
                                h1 = (aown + a090 + a135 + a180) / 4.0f / LiquidMap.MAX;
                                h4 = (aown + a180 + a225 + a270) / 4.0f / LiquidMap.MAX;
                            }

                            AddFaceIndices(meshData);
                            meshData.Vertices.Add(new Vertex(x + 0, y + h1, z + 1, leftAtlasPosition.x + 0 * atlasStep, leftAtlasPosition.y + h1 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 0, y + h2, z + 1, leftAtlasPosition.x + 0 * atlasStep, leftAtlasPosition.y + h2 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 0, y + h3, z + 0, leftAtlasPosition.x + 1 * atlasStep, leftAtlasPosition.y + h3 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 0, y + h4, z + 0, leftAtlasPosition.x + 1 * atlasStep, leftAtlasPosition.y + h4 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 0));
                            }
                        }

                        // Top face.
                        if (HasFace(x, y + 1, z)) {
                            Vector2 topAtlasPosition = (Vector2)blockDataManager.Blocks[voxelType].TopFace * atlasStep;

                            bool t000 = !IsVoxelTransparent(x + 1, y + 1, z + 0);
                            bool t090 = !IsVoxelTransparent(x + 0, y + 1, z + 1);
                            bool t180 = !IsVoxelTransparent(x - 1, y + 1, z + 0);
                            bool t270 = !IsVoxelTransparent(x + 0, y + 1, z - 1);

                            int lrtop = GetLight(x + 0, y + 1, z + 0, LightChanel.Red);
                            int lr000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Red);
                            int lr045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Red);
                            int lr090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Red);
                            int lr135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Red);
                            int lr180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Red);
                            int lr225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Red);
                            int lr270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Red);
                            int lr315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Red);

                            float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                            float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 15.0f;
                            float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 15.0f;
                            float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 15.0f;

                            int lgtop = GetLight(x + 0, y + 1, z + 0, LightChanel.Green);
                            int lg000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Green);
                            int lg045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Green);
                            int lg090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Green);
                            int lg135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Green);
                            int lg180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Green);
                            int lg225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Green);
                            int lg270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Green);
                            int lg315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Green);

                            float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                            float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 15.0f;
                            float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 15.0f;
                            float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 15.0f;

                            int lbtop = GetLight(x + 0, y + 1, z + 0, LightChanel.Blue);
                            int lb000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Blue);
                            int lb045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Blue);
                            int lb090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Blue);
                            int lb135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Blue);
                            int lb180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Blue);
                            int lb225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Blue);
                            int lb270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Blue);
                            int lb315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Blue);

                            float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                            float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 15.0f;
                            float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 15.0f;
                            float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 15.0f;

                            int lstop = GetLight(x + 0, y + 1, z + 0, LightChanel.Sun);
                            int ls000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Sun);
                            int ls045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Sun);
                            int ls090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Sun);
                            int ls135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Sun);
                            int ls180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Sun);
                            int ls225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Sun);
                            int ls270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Sun);
                            int ls315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Sun);

                            float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                            float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 15.0f;
                            float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 15.0f;
                            float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 15.0f;

                            float h1 = 1.0f;
                            float h2 = 1.0f;
                            float h3 = 1.0f;
                            float h4 = 1.0f;

                            if (atop == 0) {
                                h1 = (aown + a180 + a225 + a270) / 4.0f / LiquidMap.MAX;
                                h2 = (aown + a090 + a135 + a180) / 4.0f / LiquidMap.MAX;
                                h3 = (aown + a000 + a045 + a090) / 4.0f / LiquidMap.MAX;
                                h4 = (aown + a000 + a270 + a315) / 4.0f / LiquidMap.MAX;
                            }

                            AddFaceIndices(meshData);
                            meshData.Vertices.Add(new Vertex(x + 0, y + 1 * h1, z + 0, topAtlasPosition.x + 0 * atlasStep, topAtlasPosition.y + 0 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 1 * h2, z + 1, topAtlasPosition.x + 0 * atlasStep, topAtlasPosition.y + 1 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 1 * h3, z + 1, topAtlasPosition.x + 1 * atlasStep, topAtlasPosition.y + 1 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 1 * h4, z + 0, topAtlasPosition.x + 1 * atlasStep, topAtlasPosition.y + 0 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 0));
                            }
                        }

                        // Bottom face.
                        if (HasFace(x, y - 1, z)) {
                            Vector2 bottomAtlasPosition = (Vector2)blockDataManager.Blocks[voxelType].BottomFace * atlasStep;

                            bool t000 = !IsVoxelTransparent(x - 1, y - 1, z + 0);
                            bool t090 = !IsVoxelTransparent(x + 0, y - 1, z + 1);
                            bool t180 = !IsVoxelTransparent(x + 1, y - 1, z + 0);
                            bool t270 = !IsVoxelTransparent(x + 0, y - 1, z - 1);

                            int lrtop = GetLight(x + 0, y - 1, z + 0, LightChanel.Red);
                            int lr000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Red);
                            int lr045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Red);
                            int lr090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Red);
                            int lr135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Red);
                            int lr180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Red);
                            int lr225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Red);
                            int lr270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Red);
                            int lr315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Red);

                            float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                            float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 15.0f;
                            float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 15.0f;
                            float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 15.0f;

                            int lgtop = GetLight(x + 0, y - 1, z + 0, LightChanel.Green);
                            int lg000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Green);
                            int lg045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Green);
                            int lg090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Green);
                            int lg135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Green);
                            int lg180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Green);
                            int lg225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Green);
                            int lg270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Green);
                            int lg315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Green);

                            float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                            float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 15.0f;
                            float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 15.0f;
                            float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 15.0f;

                            int lbtop = GetLight(x + 0, y - 1, z + 0, LightChanel.Blue);
                            int lb000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Blue);
                            int lb045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Blue);
                            int lb090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Blue);
                            int lb135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Blue);
                            int lb180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Blue);
                            int lb225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Blue);
                            int lb270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Blue);
                            int lb315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Blue);

                            float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                            float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 15.0f;
                            float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 15.0f;
                            float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 15.0f;

                            int lstop = GetLight(x + 0, y - 1, z + 0, LightChanel.Sun);
                            int ls000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Sun);
                            int ls045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Sun);
                            int ls090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Sun);
                            int ls135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Sun);
                            int ls180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Sun);
                            int ls225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Sun);
                            int ls270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Sun);
                            int ls315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Sun);

                            float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                            float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 15.0f;
                            float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 15.0f;
                            float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 15.0f;

                            AddFaceIndices(meshData);
                            meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, bottomAtlasPosition.x + 0 * atlasStep, bottomAtlasPosition.y + 0 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, bottomAtlasPosition.x + 0 * atlasStep, bottomAtlasPosition.y + 1 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, bottomAtlasPosition.x + 1 * atlasStep, bottomAtlasPosition.y + 1 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, bottomAtlasPosition.x + 1 * atlasStep, bottomAtlasPosition.y + 0 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 0));
                            }
                        }

                        // Front face.
                        if (HasFace(x, y, z + 1)) {
                            Vector2 atlasPosition = (Vector2)blockDataManager.Blocks[voxelType].BackFace * atlasStep;

                            bool t000 = !IsVoxelTransparent(x - 1, y + 0, z + 1);
                            bool t090 = !IsVoxelTransparent(x + 0, y + 1, z + 1);
                            bool t180 = !IsVoxelTransparent(x + 1, y + 0, z + 1);
                            bool t270 = !IsVoxelTransparent(x + 0, y - 1, z + 1);

                            int lrtop = GetLight(x + 0, y + 0, z + 1, LightChanel.Red);
                            int lr000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Red);
                            int lr045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Red);
                            int lr090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Red);
                            int lr135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Red);
                            int lr180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Red);
                            int lr225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Red);
                            int lr270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Red);
                            int lr315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Red);

                            float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                            float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 15.0f;
                            float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 15.0f;
                            float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 15.0f;

                            int lgtop = GetLight(x + 0, y + 0, z + 1, LightChanel.Green);
                            int lg000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Green);
                            int lg045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Green);
                            int lg090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Green);
                            int lg135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Green);
                            int lg180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Green);
                            int lg225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Green);
                            int lg270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Green);
                            int lg315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Green);

                            float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                            float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 15.0f;
                            float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 15.0f;
                            float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 15.0f;

                            int lbtop = GetLight(x + 0, y + 0, z + 1, LightChanel.Blue);
                            int lb000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Blue);
                            int lb045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Blue);
                            int lb090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Blue);
                            int lb135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Blue);
                            int lb180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Blue);
                            int lb225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Blue);
                            int lb270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Blue);
                            int lb315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Blue);

                            float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                            float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 15.0f;
                            float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 15.0f;
                            float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 15.0f;

                            int lstop = GetLight(x + 0, y + 0, z + 1, LightChanel.Sun);
                            int ls000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Sun);
                            int ls045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Sun);
                            int ls090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Sun);
                            int ls135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Sun);
                            int ls180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Sun);
                            int ls225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Sun);
                            int ls270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Sun);
                            int ls315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Sun);

                            float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                            float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 15.0f;
                            float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 15.0f;
                            float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 15.0f;

                            float h1 = 0.0f;
                            float h2 = 1.0f;
                            float h3 = 1.0f;
                            float h4 = 0.0f;

                            if (atop == 0) {
                                h2 = (aown + a000 + a045 + a090) / 4.0f / LiquidMap.MAX;
                                h3 = (aown + a090 + a135 + a180) / 4.0f / LiquidMap.MAX;
                            }
                            if (a090 != 0) {
                                h1 = (aown + a000 + a045 + a090) / 4.0f / LiquidMap.MAX;
                                h4 = (aown + a090 + a135 + a180) / 4.0f / LiquidMap.MAX;
                            }

                            AddFaceIndices(meshData);
                            meshData.Vertices.Add(new Vertex(x + 1, y + h1, z + 1, atlasPosition.x + 0 * atlasStep, atlasPosition.y + h1 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 1, y + h2, z + 1, atlasPosition.x + 0 * atlasStep, atlasPosition.y + h2 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 0, y + h3, z + 1, atlasPosition.x + 1 * atlasStep, atlasPosition.y + h3 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 0, y + h4, z + 1, atlasPosition.x + 1 * atlasStep, atlasPosition.y + h4 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 1));
                            }
                        }

                        // Back face.
                        if (HasFace(x, y, z - 1)) {
                            Vector2 atlasPosition = (Vector2)blockDataManager.Blocks[voxelType].FrontFace * atlasStep;

                            bool t000 = !IsVoxelTransparent(x + 1, y + 0, z - 1);
                            bool t090 = !IsVoxelTransparent(x + 0, y + 1, z - 1);
                            bool t180 = !IsVoxelTransparent(x - 1, y + 0, z - 1);
                            bool t270 = !IsVoxelTransparent(x + 0, y - 1, z - 1);

                            int lrtop = GetLight(x + 0, y + 0, z - 1, LightChanel.Red);
                            int lr000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Red);
                            int lr045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Red);
                            int lr090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Red);
                            int lr135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Red);
                            int lr180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Red);
                            int lr225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Red);
                            int lr270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Red);
                            int lr315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Red);

                            float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                            float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 15.0f;
                            float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 15.0f;
                            float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 15.0f;

                            int lgtop = GetLight(x + 0, y + 0, z - 1, LightChanel.Green);
                            int lg000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Green);
                            int lg045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Green);
                            int lg090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Green);
                            int lg135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Green);
                            int lg180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Green);
                            int lg225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Green);
                            int lg270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Green);
                            int lg315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Green);

                            float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                            float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 15.0f;
                            float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 15.0f;
                            float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 15.0f;

                            int lbtop = GetLight(x + 0, y + 0, z - 1, LightChanel.Blue);
                            int lb000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Blue);
                            int lb045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Blue);
                            int lb090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Blue);
                            int lb135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Blue);
                            int lb180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Blue);
                            int lb225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Blue);
                            int lb270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Blue);
                            int lb315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Blue);

                            float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                            float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 15.0f;
                            float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 15.0f;
                            float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 15.0f;

                            int lstop = GetLight(x + 0, y + 0, z - 1, LightChanel.Sun);
                            int ls000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Sun);
                            int ls045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Sun);
                            int ls090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Sun);
                            int ls135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Sun);
                            int ls180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Sun);
                            int ls225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Sun);
                            int ls270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Sun);
                            int ls315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Sun);

                            float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                            float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 15.0f;
                            float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 15.0f;
                            float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 15.0f;

                            float h1 = 0.0f;
                            float h2 = 1.0f;
                            float h3 = 1.0f;
                            float h4 = 0.0f;

                            if (atop == 0) {
                                h2 = (aown + a180 + a225 + a270) / 4.0f / LiquidMap.MAX;
                                h3 = (aown + a000 + a270 + a315) / 4.0f / LiquidMap.MAX;
                            }
                            if (a270 != 0) {
                                h1 = (aown + a180 + a225 + a270) / 4.0f / LiquidMap.MAX;
                                h4 = (aown + a000 + a270 + a315) / 4.0f / LiquidMap.MAX;
                            }

                            AddFaceIndices(meshData);
                            meshData.Vertices.Add(new Vertex(x + 0, y + h1, z + 0, atlasPosition.x + 0 * atlasStep, atlasPosition.y + h1 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 0, y + h2, z + 0, atlasPosition.x + 0 * atlasStep, atlasPosition.y + h2 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 1, y + h3, z + 0, atlasPosition.x + 1 * atlasStep, atlasPosition.y + h3 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 1, y + h4, z + 0, atlasPosition.x + 1 * atlasStep, atlasPosition.y + h4 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 0));
                            }
                        }
                    } else {
                        bool HasFace(int x, int y, int z) {
                            return IsVoxelTransparent(x, y, z) && GetVoxel(x, y, z) != voxelType;
                        }

                        // Right face.
                        if (HasFace(x + 1, y, z)) {
                            Vector2 atlasPosition = (Vector2)blockDataManager.Blocks[voxelType].RightFace * atlasStep;

                            bool t000 = !IsVoxelTransparent(x + 1, y + 0, z + 1);
                            bool t090 = !IsVoxelTransparent(x + 1, y + 1, z + 0);
                            bool t180 = !IsVoxelTransparent(x + 1, y + 0, z - 1);
                            bool t270 = !IsVoxelTransparent(x + 1, y - 1, z + 0);

                            int lrtop = GetLight(x + 1, y + 0, z + 0, LightChanel.Red);
                            int lr000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Red);
                            int lr045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Red);
                            int lr090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Red);
                            int lr135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Red);
                            int lr180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Red);
                            int lr225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Red);
                            int lr270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Red);
                            int lr315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Red);

                            float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                            float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 15.0f;
                            float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 15.0f;
                            float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 15.0f;

                            int lgtop = GetLight(x + 1, y + 0, z + 0, LightChanel.Green);
                            int lg000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Green);
                            int lg045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Green);
                            int lg090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Green);
                            int lg135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Green);
                            int lg180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Green);
                            int lg225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Green);
                            int lg270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Green);
                            int lg315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Green);

                            float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                            float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 15.0f;
                            float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 15.0f;
                            float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 15.0f;

                            int lbtop = GetLight(x + 1, y + 0, z + 0, LightChanel.Blue);
                            int lb000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Blue);
                            int lb045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Blue);
                            int lb090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Blue);
                            int lb135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Blue);
                            int lb180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Blue);
                            int lb225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Blue);
                            int lb270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Blue);
                            int lb315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Blue);

                            float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                            float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 15.0f;
                            float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 15.0f;
                            float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 15.0f;

                            int lstop = GetLight(x + 1, y + 0, z + 0, LightChanel.Sun);
                            int ls000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Sun);
                            int ls045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Sun);
                            int ls090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Sun);
                            int ls135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Sun);
                            int ls180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Sun);
                            int ls225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Sun);
                            int ls270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Sun);
                            int ls315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Sun);

                            float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                            float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 15.0f;
                            float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 15.0f;
                            float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 15.0f;

                            AddFaceIndices(meshData);
                            meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, atlasPosition.x + 0 * atlasStep, atlasPosition.y + 0 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, atlasPosition.x + 0 * atlasStep, atlasPosition.y + 1 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, atlasPosition.x + 1 * atlasStep, atlasPosition.y + 1 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, atlasPosition.x + 1 * atlasStep, atlasPosition.y + 0 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 1));
                            }
                        }

                        // Left face.
                        if (HasFace(x - 1, y, z)) {
                            Vector2 leftAtlasPosition = (Vector2)blockDataManager.Blocks[voxelType].LeftFace * atlasStep;

                            bool t000 = !IsVoxelTransparent(x - 1, y + 0, z - 1);
                            bool t090 = !IsVoxelTransparent(x - 1, y + 1, z + 0);
                            bool t180 = !IsVoxelTransparent(x - 1, y + 0, z + 1);
                            bool t270 = !IsVoxelTransparent(x - 1, y - 1, z + 0);

                            int lrtop = GetLight(x - 1, y + 0, z + 0, LightChanel.Red);
                            int lr000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Red);
                            int lr045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Red);
                            int lr090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Red);
                            int lr135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Red);
                            int lr180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Red);
                            int lr225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Red);
                            int lr270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Red);
                            int lr315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Red);

                            float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                            float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 15.0f;
                            float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 15.0f;
                            float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 15.0f;

                            int lgtop = GetLight(x - 1, y + 0, z + 0, LightChanel.Green);
                            int lg000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Green);
                            int lg045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Green);
                            int lg090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Green);
                            int lg135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Green);
                            int lg180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Green);
                            int lg225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Green);
                            int lg270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Green);
                            int lg315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Green);

                            float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                            float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 15.0f;
                            float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 15.0f;
                            float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 15.0f;

                            int lbtop = GetLight(x - 1, y + 0, z + 0, LightChanel.Blue);
                            int lb000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Blue);
                            int lb045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Blue);
                            int lb090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Blue);
                            int lb135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Blue);
                            int lb180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Blue);
                            int lb225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Blue);
                            int lb270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Blue);
                            int lb315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Blue);

                            float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                            float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 15.0f;
                            float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 15.0f;
                            float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 15.0f;

                            int lstop = GetLight(x - 1, y + 0, z + 0, LightChanel.Sun);
                            int ls000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Sun);
                            int ls045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Sun);
                            int ls090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Sun);
                            int ls135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Sun);
                            int ls180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Sun);
                            int ls225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Sun);
                            int ls270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Sun);
                            int ls315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Sun);

                            float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                            float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 15.0f;
                            float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 15.0f;
                            float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 15.0f;

                            AddFaceIndices(meshData);
                            meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, leftAtlasPosition.x + 0 * atlasStep, leftAtlasPosition.y + 0 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, leftAtlasPosition.x + 0 * atlasStep, leftAtlasPosition.y + 1 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, leftAtlasPosition.x + 1 * atlasStep, leftAtlasPosition.y + 1 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, leftAtlasPosition.x + 1 * atlasStep, leftAtlasPosition.y + 0 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 0));
                            }
                        }

                        // Top face.
                        if (HasFace(x, y + 1, z)) {
                            Vector2 topAtlasPosition = (Vector2)blockDataManager.Blocks[voxelType].TopFace * atlasStep;

                            bool t000 = !IsVoxelTransparent(x + 1, y + 1, z + 0);
                            bool t090 = !IsVoxelTransparent(x + 0, y + 1, z + 1);
                            bool t180 = !IsVoxelTransparent(x - 1, y + 1, z + 0);
                            bool t270 = !IsVoxelTransparent(x + 0, y + 1, z - 1);

                            int lrtop = GetLight(x + 0, y + 1, z + 0, LightChanel.Red);
                            int lr000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Red);
                            int lr045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Red);
                            int lr090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Red);
                            int lr135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Red);
                            int lr180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Red);
                            int lr225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Red);
                            int lr270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Red);
                            int lr315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Red);

                            float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                            float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 15.0f;
                            float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 15.0f;
                            float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 15.0f;

                            int lgtop = GetLight(x + 0, y + 1, z + 0, LightChanel.Green);
                            int lg000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Green);
                            int lg045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Green);
                            int lg090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Green);
                            int lg135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Green);
                            int lg180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Green);
                            int lg225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Green);
                            int lg270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Green);
                            int lg315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Green);

                            float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                            float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 15.0f;
                            float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 15.0f;
                            float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 15.0f;

                            int lbtop = GetLight(x + 0, y + 1, z + 0, LightChanel.Blue);
                            int lb000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Blue);
                            int lb045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Blue);
                            int lb090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Blue);
                            int lb135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Blue);
                            int lb180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Blue);
                            int lb225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Blue);
                            int lb270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Blue);
                            int lb315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Blue);

                            float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                            float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 15.0f;
                            float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 15.0f;
                            float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 15.0f;

                            int lstop = GetLight(x + 0, y + 1, z + 0, LightChanel.Sun);
                            int ls000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Sun);
                            int ls045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Sun);
                            int ls090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Sun);
                            int ls135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Sun);
                            int ls180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Sun);
                            int ls225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Sun);
                            int ls270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Sun);
                            int ls315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Sun);

                            float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                            float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 15.0f;
                            float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 15.0f;
                            float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 15.0f;

                            AddFaceIndices(meshData);
                            meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, topAtlasPosition.x + 0 * atlasStep, topAtlasPosition.y + 0 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, topAtlasPosition.x + 0 * atlasStep, topAtlasPosition.y + 1 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, topAtlasPosition.x + 1 * atlasStep, topAtlasPosition.y + 1 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, topAtlasPosition.x + 1 * atlasStep, topAtlasPosition.y + 0 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 0));
                            }
                        }

                        // Bottom face.
                        if (HasFace(x, y - 1, z)) {
                            Vector2 bottomAtlasPosition = (Vector2)blockDataManager.Blocks[voxelType].BottomFace * atlasStep;

                            bool t000 = !IsVoxelTransparent(x - 1, y - 1, z + 0);
                            bool t090 = !IsVoxelTransparent(x + 0, y - 1, z + 1);
                            bool t180 = !IsVoxelTransparent(x + 1, y - 1, z + 0);
                            bool t270 = !IsVoxelTransparent(x + 0, y - 1, z - 1);

                            int lrtop = GetLight(x + 0, y - 1, z + 0, LightChanel.Red);
                            int lr000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Red);
                            int lr045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Red);
                            int lr090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Red);
                            int lr135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Red);
                            int lr180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Red);
                            int lr225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Red);
                            int lr270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Red);
                            int lr315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Red);

                            float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                            float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 15.0f;
                            float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 15.0f;
                            float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 15.0f;

                            int lgtop = GetLight(x + 0, y - 1, z + 0, LightChanel.Green);
                            int lg000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Green);
                            int lg045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Green);
                            int lg090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Green);
                            int lg135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Green);
                            int lg180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Green);
                            int lg225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Green);
                            int lg270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Green);
                            int lg315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Green);

                            float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                            float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 15.0f;
                            float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 15.0f;
                            float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 15.0f;

                            int lbtop = GetLight(x + 0, y - 1, z + 0, LightChanel.Blue);
                            int lb000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Blue);
                            int lb045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Blue);
                            int lb090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Blue);
                            int lb135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Blue);
                            int lb180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Blue);
                            int lb225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Blue);
                            int lb270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Blue);
                            int lb315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Blue);

                            float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                            float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 15.0f;
                            float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 15.0f;
                            float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 15.0f;

                            int lstop = GetLight(x + 0, y - 1, z + 0, LightChanel.Sun);
                            int ls000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Sun);
                            int ls045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Sun);
                            int ls090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Sun);
                            int ls135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Sun);
                            int ls180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Sun);
                            int ls225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Sun);
                            int ls270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Sun);
                            int ls315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Sun);

                            float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                            float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 15.0f;
                            float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 15.0f;
                            float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 15.0f;

                            AddFaceIndices(meshData);
                            meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, bottomAtlasPosition.x + 0 * atlasStep, bottomAtlasPosition.y + 0 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, bottomAtlasPosition.x + 0 * atlasStep, bottomAtlasPosition.y + 1 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, bottomAtlasPosition.x + 1 * atlasStep, bottomAtlasPosition.y + 1 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, bottomAtlasPosition.x + 1 * atlasStep, bottomAtlasPosition.y + 0 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 0));
                            }
                        }

                        // Front face.
                        if (HasFace(x, y, z + 1)) {
                            Vector2 backAtlasPosition = (Vector2)blockDataManager.Blocks[voxelType].BackFace * atlasStep;

                            bool t000 = !IsVoxelTransparent(x - 1, y + 0, z + 1);
                            bool t090 = !IsVoxelTransparent(x + 0, y + 1, z + 1);
                            bool t180 = !IsVoxelTransparent(x + 1, y + 0, z + 1);
                            bool t270 = !IsVoxelTransparent(x + 0, y - 1, z + 1);

                            int lrtop = GetLight(x + 0, y + 0, z + 1, LightChanel.Red);
                            int lr000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Red);
                            int lr045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Red);
                            int lr090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Red);
                            int lr135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Red);
                            int lr180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Red);
                            int lr225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Red);
                            int lr270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Red);
                            int lr315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Red);

                            float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                            float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 15.0f;
                            float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 15.0f;
                            float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 15.0f;

                            int lgtop = GetLight(x + 0, y + 0, z + 1, LightChanel.Green);
                            int lg000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Green);
                            int lg045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Green);
                            int lg090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Green);
                            int lg135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Green);
                            int lg180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Green);
                            int lg225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Green);
                            int lg270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Green);
                            int lg315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Green);

                            float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                            float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 15.0f;
                            float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 15.0f;
                            float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 15.0f;

                            int lbtop = GetLight(x + 0, y + 0, z + 1, LightChanel.Blue);
                            int lb000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Blue);
                            int lb045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Blue);
                            int lb090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Blue);
                            int lb135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Blue);
                            int lb180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Blue);
                            int lb225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Blue);
                            int lb270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Blue);
                            int lb315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Blue);

                            float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                            float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 15.0f;
                            float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 15.0f;
                            float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 15.0f;

                            int lstop = GetLight(x + 0, y + 0, z + 1, LightChanel.Sun);
                            int ls000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Sun);
                            int ls045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Sun);
                            int ls090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Sun);
                            int ls135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Sun);
                            int ls180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Sun);
                            int ls225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Sun);
                            int ls270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Sun);
                            int ls315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Sun);

                            float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                            float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 15.0f;
                            float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 15.0f;
                            float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 15.0f;

                            AddFaceIndices(meshData);
                            meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, backAtlasPosition.x + 0 * atlasStep, backAtlasPosition.y + 0 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, backAtlasPosition.x + 0 * atlasStep, backAtlasPosition.y + 1 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, backAtlasPosition.x + 1 * atlasStep, backAtlasPosition.y + 1 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, backAtlasPosition.x + 1 * atlasStep, backAtlasPosition.y + 0 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 1));
                            }
                        }

                        // Back face.
                        if (HasFace(x, y, z - 1)) {
                            Vector2 frontAtlasPosition = (Vector2)blockDataManager.Blocks[voxelType].FrontFace * atlasStep;

                            bool t000 = !IsVoxelTransparent(x + 1, y + 0, z - 1);
                            bool t090 = !IsVoxelTransparent(x + 0, y + 1, z - 1);
                            bool t180 = !IsVoxelTransparent(x - 1, y + 0, z - 1);
                            bool t270 = !IsVoxelTransparent(x + 0, y - 1, z - 1);

                            int lrtop = GetLight(x + 0, y + 0, z - 1, LightChanel.Red);
                            int lr000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Red);
                            int lr045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Red);
                            int lr090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Red);
                            int lr135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Red);
                            int lr180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Red);
                            int lr225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Red);
                            int lr270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Red);
                            int lr315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Red);

                            float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 15.0f;
                            float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 15.0f;
                            float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 15.0f;
                            float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 15.0f;

                            int lgtop = GetLight(x + 0, y + 0, z - 1, LightChanel.Green);
                            int lg000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Green);
                            int lg045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Green);
                            int lg090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Green);
                            int lg135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Green);
                            int lg180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Green);
                            int lg225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Green);
                            int lg270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Green);
                            int lg315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Green);

                            float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 15.0f;
                            float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 15.0f;
                            float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 15.0f;
                            float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 15.0f;

                            int lbtop = GetLight(x + 0, y + 0, z - 1, LightChanel.Blue);
                            int lb000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Blue);
                            int lb045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Blue);
                            int lb090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Blue);
                            int lb135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Blue);
                            int lb180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Blue);
                            int lb225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Blue);
                            int lb270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Blue);
                            int lb315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Blue);

                            float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 15.0f;
                            float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 15.0f;
                            float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 15.0f;
                            float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 15.0f;

                            int lstop = GetLight(x + 0, y + 0, z - 1, LightChanel.Sun);
                            int ls000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Sun);
                            int ls045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Sun);
                            int ls090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Sun);
                            int ls135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Sun);
                            int ls180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Sun);
                            int ls225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Sun);
                            int ls270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Sun);
                            int ls315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Sun);

                            float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 15.0f;
                            float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 15.0f;
                            float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 15.0f;
                            float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 15.0f;

                            AddFaceIndices(meshData);
                            meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, frontAtlasPosition.x + 0 * atlasStep, frontAtlasPosition.y + 0 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, frontAtlasPosition.x + 0 * atlasStep, frontAtlasPosition.y + 1 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, frontAtlasPosition.x + 1 * atlasStep, frontAtlasPosition.y + 1 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, frontAtlasPosition.x + 1 * atlasStep, frontAtlasPosition.y + 0 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 0));
                            }
                        }
                    }
                }
            });

            return result;
        }
    }
}