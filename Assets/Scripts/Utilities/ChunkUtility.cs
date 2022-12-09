using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Minecraft.Utilities {
    public static class ChunkUtility {
        public static void For(Action<int, int, int> action) {
            for (int y = 0; y < Chunk.SIZE; y++)
                for (int x = 0; x < Chunk.SIZE; x++)
                    for (int z = 0; z < Chunk.SIZE; z++)
                        action(y, x, z);
        }

        public static void For(Action<Vector3Int> action) {
            for (int y = 0; y < Chunk.SIZE; y++)
                for (int x = 0; x < Chunk.SIZE; x++)
                    for (int z = 0; z < Chunk.SIZE; z++)
                        action(new Vector3Int(x, y, z));
        }

        private static readonly object lockObject = new();

        public static void ParallelFor(Action<int, int, int> action) {
            Parallel.For(0, Chunk.VOLUME, (index, state) => {
                int z = index / (Chunk.SIZE * Chunk.SIZE);
                index -= z * Chunk.SIZE * Chunk.SIZE;
                int y = index / Chunk.SIZE;
                int x = index % Chunk.SIZE;
                action(x, y, z);
            });
        }

        public static ConcurrentDictionary<MaterialType, MeshData> GenerateMeshData(World world, ChunkData chunkData, BlockDataManager blockDataManager) {
            BlockType GetVoxel(int x, int y, int z) {
                Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(chunkData.Coordinate, new Vector3Int(x, y, z));
                return world.GetVoxel(blockCoordinate);
            }

            int GetLight(int x, int y, int z, LightChanel chanel) {
                Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(chunkData.Coordinate, new Vector3Int(x, y, z));
                return world.GetLightLevel(blockCoordinate, chanel);
            }

            byte GetLiquidAmount(int x, int y, int z, BlockType liquidType) {
                Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(chunkData.Coordinate, new Vector3Int(x, y, z));
                return world.GetLiquidAmount(blockCoordinate, liquidType);
            }

            bool IsVoxelSolid(int x, int y, int z) {
                return blockDataManager.Data[GetVoxel(x, y, z)].IsSolid;
            }

            bool IsVoxelTransparent(int x, int y, int z) {
                return blockDataManager.Data[GetVoxel(x, y, z)].IsTransparent;
            }

            void AddFaceIndices(MeshData meshData, float aof1, float aof2, float aof3, float aof4) {
                int vertexCount = meshData.Vertices.Count;
                if (aof1 + aof3 > aof2 + aof4) {
                    // Normal quad.
                    meshData.Indices.Add((ushort)(0 + vertexCount));
                    meshData.Indices.Add((ushort)(1 + vertexCount));
                    meshData.Indices.Add((ushort)(2 + vertexCount));
                    meshData.Indices.Add((ushort)(0 + vertexCount));
                    meshData.Indices.Add((ushort)(2 + vertexCount));
                    meshData.Indices.Add((ushort)(3 + vertexCount));
                } else {
                    // Fliped quad.
                    meshData.Indices.Add((ushort)(0 + vertexCount));
                    meshData.Indices.Add((ushort)(1 + vertexCount));
                    meshData.Indices.Add((ushort)(3 + vertexCount));
                    meshData.Indices.Add((ushort)(3 + vertexCount));
                    meshData.Indices.Add((ushort)(1 + vertexCount));
                    meshData.Indices.Add((ushort)(2 + vertexCount));
                }
            }

            void AddFaceColliderIndices(MeshData meshData) {
                int vertexCount = meshData.ColliderVertices.Count;
                lock (lockObject) {
                    meshData.ColliderIndices.Add((ushort)(0 + vertexCount));
                    meshData.ColliderIndices.Add((ushort)(1 + vertexCount));
                    meshData.ColliderIndices.Add((ushort)(2 + vertexCount));
                    meshData.ColliderIndices.Add((ushort)(0 + vertexCount));
                    meshData.ColliderIndices.Add((ushort)(2 + vertexCount));
                    meshData.ColliderIndices.Add((ushort)(3 + vertexCount));
                }
            }

            ConcurrentDictionary<MaterialType, MeshData> result = new();

            ParallelFor((x, y, z) => {
                BlockType voxelType = chunkData.BlockMap[x, y, z];

                if (voxelType != BlockType.Air) {
                    var localBlockCoordinate = new Vector3Int(x, y, z);
                    MaterialType materialType = blockDataManager.Data[voxelType].MaterialType;
                    if (!result.ContainsKey(materialType))
                        result.TryAdd(materialType, new MeshData());

                    float atlasStep = 16.0f / 256.0f;
                    bool isSolid = IsVoxelSolid(x, y, z);
                    bool isLiquid = blockDataManager.Data[voxelType].IsLiquid;
                    var meshData = result[materialType];

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

                    bool s000 = IsVoxelSolid(x + 1, y + 0, z + 0);
                    bool s045 = IsVoxelSolid(x + 1, y + 0, z + 1);
                    bool s090 = IsVoxelSolid(x + 0, y + 0, z + 1);
                    bool s135 = IsVoxelSolid(x - 1, y + 0, z + 1);
                    bool s180 = IsVoxelSolid(x - 1, y + 0, z + 0);
                    bool s225 = IsVoxelSolid(x - 1, y + 0, z - 1);
                    bool s270 = IsVoxelSolid(x + 0, y + 0, z - 1);
                    bool s315 = IsVoxelSolid(x + 1, y + 0, z - 1);

                    var afbk = LiquidMap.MAX;

                    Func<int, int, int, bool> hasFace = null;
                    if (isLiquid) {
                        hasFace = (int x, int y, int z) => {
                            bool isTop = y - localBlockCoordinate.y == 1;
							if (GetVoxel(x, y, z) == voxelType) {
                                var side = GetLiquidAmount(x, y, z, voxelType);
                                return atop != 0 && side < aown && !isTop;
                            } else {
                                return IsVoxelTransparent(x, y, z) || isTop;
							}
                        };
                    } else {
                        hasFace = (int x, int y, int z) => {
                            return IsVoxelTransparent(x, y, z) && GetVoxel(x, y, z) != voxelType;
                        };
                    }

                    // Right face.
                    if (hasFace(x + 1, y, z)) {
                        Vector2 atlasPosition = (Vector2)blockDataManager.Data[voxelType].TexturingData.RightFace * atlasStep;

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

                        if (isLiquid) {
                            if (atop == 0) {
                                h2 = (aown
                                + (s000 ? afbk : a000)
                                + (s270 ? afbk : a270)
                                + (s315 ? afbk : a315)) / 4.0f / LiquidMap.MAX;
                                h3 = (aown
                                + (s000 ? afbk : a000)
                                + (s045 ? afbk : a045)
                                + (s090 ? afbk : a090)) / 4.0f / LiquidMap.MAX;
                            }
                            if (a000 != 0) {
                                h1 = (aown
                                + (s000 ? afbk : a000)
                                + (s270 ? afbk : a270)
                                + (s315 ? afbk : a315)) / 4.0f / LiquidMap.MAX;
                                h4 = (aown
                                + (s000 ? afbk : a000)
                                + (s045 ? afbk : a045)
                                + (s090 ? afbk : a090)) / 4.0f / LiquidMap.MAX;
                            }
                        }

                        float aof1 = lr1 + lg1 + lb1 + ls1;
                        float aof2 = lr2 + lg2 + lb2 + ls2;
                        float aof3 = lr3 + lg3 + lb3 + ls3;
                        float aof4 = lr4 + lg4 + lb4 + ls4;

                        lock (lockObject) {
                            AddFaceIndices(meshData, aof1, aof2, aof3, aof4);
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
                    }

                    // Left face.
                    if (hasFace(x - 1, y, z)) {
                        Vector2 atlasPosition = (Vector2)blockDataManager.Data[voxelType].TexturingData.LeftFace * atlasStep;

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

                        if (isLiquid) {
                            if (atop == 0) {
                                h2 = (aown
                                + (s090 ? afbk : a090)
                                + (s135 ? afbk : a135)
                                + (s180 ? afbk : a180)) / 4.0f / LiquidMap.MAX;
                                h3 = (aown
                                + (s180 ? afbk : a180)
                                + (s225 ? afbk : a225)
                                + (s270 ? afbk : a270)) / 4.0f / LiquidMap.MAX;
                            }
                            if (a180 != 0) {
                                h1 = (aown
                                + (s090 ? afbk : a090)
                                + (s135 ? afbk : a135)
                                + (s180 ? afbk : a180)) / 4.0f / LiquidMap.MAX;
                                h4 = (aown
                                + (s180 ? afbk : a180)
                                + (s225 ? afbk : a225)
                                + (s270 ? afbk : a270)) / 4.0f / LiquidMap.MAX;
                            }
                        }

                        float aof1 = lr1 + lg1 + lb1 + ls1;
                        float aof2 = lr2 + lg2 + lb2 + ls2;
                        float aof3 = lr3 + lg3 + lb3 + ls3;
                        float aof4 = lr4 + lg4 + lb4 + ls4;

                        lock (lockObject) {
                            AddFaceIndices(meshData, aof1, aof2, aof3, aof4);
                            meshData.Vertices.Add(new Vertex(x + 0, y + h1, z + 1, atlasPosition.x + 0 * atlasStep, atlasPosition.y + h1 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 0, y + h2, z + 1, atlasPosition.x + 0 * atlasStep, atlasPosition.y + h2 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 0, y + h3, z + 0, atlasPosition.x + 1 * atlasStep, atlasPosition.y + h3 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 0, y + h4, z + 0, atlasPosition.x + 1 * atlasStep, atlasPosition.y + h4 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 0));
                            }
                        }
                    }

                    // Top face.
                    if (hasFace(x, y + 1, z)) {
                        Vector2 atlasPosition = (Vector2)blockDataManager.Data[voxelType].TexturingData.TopFace * atlasStep;

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

                        if (isLiquid) {
                            if (atop == 0) {
                                h1 = (aown
                                + (s180 ? afbk : a180)
                                + (s225 ? afbk : a225)
                                + (s270 ? afbk : a270)) / 4.0f / LiquidMap.MAX;
                                h2 = (aown
                                + (s090 ? afbk : a090)
                                + (s135 ? afbk : a135)
                                + (s180 ? afbk : a180)) / 4.0f / LiquidMap.MAX;
                                h3 = (aown
                                + (s000 ? afbk : a000)
                                + (s045 ? afbk : a045)
                                + (s090 ? afbk : a090)) / 4.0f / LiquidMap.MAX;
                                h4 = (aown
                                + (s000 ? afbk : a000)
                                + (s270 ? afbk : a270)
                                + (s315 ? afbk : a315)) / 4.0f / LiquidMap.MAX;
                            }
                        }

                        float aof1 = lr1 + lg1 + lb1 + ls1;
                        float aof2 = lr2 + lg2 + lb2 + ls2;
                        float aof3 = lr3 + lg3 + lb3 + ls3;
                        float aof4 = lr4 + lg4 + lb4 + ls4;

                        lock (lockObject) {
                            AddFaceIndices(meshData, aof1, aof2, aof3, aof4);
                            meshData.Vertices.Add(new Vertex(x + 0, y + 1 * h1, z + 0, atlasPosition.x + 0 * atlasStep, atlasPosition.y + 0 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 1 * h2, z + 1, atlasPosition.x + 0 * atlasStep, atlasPosition.y + 1 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 1 * h3, z + 1, atlasPosition.x + 1 * atlasStep, atlasPosition.y + 1 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 1 * h4, z + 0, atlasPosition.x + 1 * atlasStep, atlasPosition.y + 0 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 0));
                            }
                        }
                    }

                    // Bottom face.
                    if (hasFace(x, y - 1, z)) {
                        Vector2 atlasPosition = (Vector2)blockDataManager.Data[voxelType].TexturingData.BottomFace * atlasStep;

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

                        float aof1 = lr1 + lg1 + lb1 + ls1;
                        float aof2 = lr2 + lg2 + lb2 + ls2;
                        float aof3 = lr3 + lg3 + lb3 + ls3;
                        float aof4 = lr4 + lg4 + lb4 + ls4;

                        lock (lockObject) {
                            AddFaceIndices(meshData, aof1, aof2, aof3, aof4);
                            meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, atlasPosition.x + 0 * atlasStep, atlasPosition.y + 0 * atlasStep, lr1, lg1, lb1, ls1));
                            meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, atlasPosition.x + 0 * atlasStep, atlasPosition.y + 1 * atlasStep, lr2, lg2, lb2, ls2));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, atlasPosition.x + 1 * atlasStep, atlasPosition.y + 1 * atlasStep, lr3, lg3, lb3, ls3));
                            meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, atlasPosition.x + 1 * atlasStep, atlasPosition.y + 0 * atlasStep, lr4, lg4, lb4, ls4));

                            if (isSolid) {
                                AddFaceColliderIndices(meshData);
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 0));
                                meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 1));
                                meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 0));
                            }
                        }
                    }

                    // Front face.
                    if (hasFace(x, y, z + 1)) {
                        Vector2 atlasPosition = (Vector2)blockDataManager.Data[voxelType].TexturingData.BackFace * atlasStep;

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

                        if (isLiquid) {
                            if (atop == 0) {
                                h2 = (aown
                                + (s000 ? afbk : a000)
                                + (s045 ? afbk : a045)
                                + (s090 ? afbk : a090)) / 4.0f / LiquidMap.MAX;
                                h3 = (aown
                                + (s090 ? afbk : a090)
                                + (s135 ? afbk : a135)
                                + (s180 ? afbk : a180)) / 4.0f / LiquidMap.MAX;
                            }
                            if (a090 != 0) {
                                h1 = (aown
                                + (s000 ? afbk : a000)
                                + (s045 ? afbk : a045)
                                + (s090 ? afbk : a090)) / 4.0f / LiquidMap.MAX;
                                h4 = (aown
                                + (s090 ? afbk : a090)
                                + (s135 ? afbk : a135)
                                + (s180 ? afbk : a180)) / 4.0f / LiquidMap.MAX;
                            }
                        }

                        float aof1 = lr1 + lg1 + lb1 + ls1;
                        float aof2 = lr2 + lg2 + lb2 + ls2;
                        float aof3 = lr3 + lg3 + lb3 + ls3;
                        float aof4 = lr4 + lg4 + lb4 + ls4;

                        lock (lockObject) {
                            AddFaceIndices(meshData, aof1, aof2, aof3, aof4);
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
                    }

                    // Back face.
                    if (hasFace(x, y, z - 1)) {
                        Vector2 atlasPosition = (Vector2)blockDataManager.Data[voxelType].TexturingData.FrontFace * atlasStep;

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

                        if (isLiquid) {
                            if (atop == 0) {
                                h2 = (aown
                                + (s180 ? afbk : a180)
                                + (s225 ? afbk : a225)
                                + (s270 ? afbk : a270)) / 4.0f / LiquidMap.MAX;
                                h3 = (aown
                                + (s000 ? afbk : a000)
                                + (s270 ? afbk : a270)
                                + (s315 ? afbk : a315)) / 4.0f / LiquidMap.MAX;
                            }
                            if (a270 != 0) {
                                h1 = (aown
                                + (s180 ? afbk : a180)
                                + (s225 ? afbk : a225)
                                + (s270 ? afbk : a270)) / 4.0f / LiquidMap.MAX;
                                h4 = (aown
                                + (s000 ? afbk : a000)
                                + (s270 ? afbk : a270)
                                + (s315 ? afbk : a315)) / 4.0f / LiquidMap.MAX;
                            }
                        }

                        float aof1 = lr1 + lg1 + lb1 + ls1;
                        float aof2 = lr2 + lg2 + lb2 + ls2;
                        float aof3 = lr3 + lg3 + lb3 + ls3;
                        float aof4 = lr4 + lg4 + lb4 + ls4;

                        lock (lockObject) {
                            AddFaceIndices(meshData, aof1, aof2, aof3, aof4);
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
                    }
                }
            });

            return result;
        }
    }
}