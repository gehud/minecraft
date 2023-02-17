using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

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

        private const int SIZE_X_SIZE = Chunk.SIZE * Chunk.SIZE;

        private static readonly object lockObject = new();

        public static void ParallelFor(Action<int, int, int> action) {
            Parallel.For(0, Chunk.VOLUME, (index, state) => {
                int z = index / SIZE_X_SIZE;
                index -= z * SIZE_X_SIZE;
                int y = index / Chunk.SIZE;
                int x = index % Chunk.SIZE;
                action(x, y, z);
            });
        }

        public static ConcurrentDictionary<MaterialType, MeshData> GenerateMeshData(World world, Chunk chunkData, BlockDataProvider blockDataProvider) {
            BlockType GetBlock(int x, int y, int z) {
                Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(chunkData.Coordinate, new Vector3Int(x, y, z));
                return world.GetBlock(blockCoordinate);
            }

            int GetLight(int x, int y, int z, LightChanel chanel) {
                Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(chunkData.Coordinate, new Vector3Int(x, y, z));
                return world.GetLightLevel(blockCoordinate, chanel);
            }

            byte GetLiquidAmount(int x, int y, int z, BlockType liquidType) {
                Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(chunkData.Coordinate, new Vector3Int(x, y, z));
                return world.GetLiquidAmount(blockCoordinate, liquidType);
            }

            bool IsSolid(int x, int y, int z) {
                return blockDataProvider.Get(GetBlock(x, y, z)).IsSolid;
            }

            bool IsLiquid(int x, int y, int z) {
                return blockDataProvider.Get(GetBlock(x, y, z)).IsLiquid;
            }

            bool IsVoxelTransparent(int x, int y, int z) {
                return blockDataProvider.Get(GetBlock(x, y, z)).IsTransparent;
            }

            void AddFaceIndices(MeshData meshData, float aof1, float aof2, float aof3, float aof4, bool force = false, bool fliped = false) {
                int vertexCount = meshData.Vertices.Count;
                if (force && fliped || aof1 + aof3 < aof2 + aof4) {
                    // Fliped quad.
                    meshData.Indices.Add((ushort)(0 + vertexCount));
                    meshData.Indices.Add((ushort)(1 + vertexCount));
                    meshData.Indices.Add((ushort)(3 + vertexCount));
                    meshData.Indices.Add((ushort)(3 + vertexCount));
                    meshData.Indices.Add((ushort)(1 + vertexCount));
                    meshData.Indices.Add((ushort)(2 + vertexCount));
                } else {
                    // Normal quad.
                    meshData.Indices.Add((ushort)(0 + vertexCount));
                    meshData.Indices.Add((ushort)(1 + vertexCount));
                    meshData.Indices.Add((ushort)(2 + vertexCount));
                    meshData.Indices.Add((ushort)(0 + vertexCount));
                    meshData.Indices.Add((ushort)(2 + vertexCount));
                    meshData.Indices.Add((ushort)(3 + vertexCount));
                }
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

            ConcurrentDictionary<MaterialType, MeshData> result = new();
#if CHUNK_UTILITY_PARALLEL_FOR
            ParallelFor((x, y, z) => {
#else
            For((x, y, z) => { 
#endif
                BlockType blockType = chunkData.BlockMap[x, y, z];

                if (blockType != BlockType.Air) {
                    var localBlockCoordinate = new Vector3Int(x, y, z);
                    var blockData = blockDataProvider.Get(blockType);
					MaterialType materialType = blockData.MaterialType;
                    if (!result.ContainsKey(materialType))
                        result.TryAdd(materialType, new MeshData());

                    float atlasStep = 16.0f / 256.0f;
                    bool isSolid = IsSolid(x, y, z);
                    bool isLiquid = blockData.IsLiquid;
                    var meshData = result[materialType];

                    byte aown = GetLiquidAmount(x + 0, y + 0, z + 0, blockType);
                    byte atop = GetLiquidAmount(x + 0, y + 1, z + 0, blockType);
                    byte abot = GetLiquidAmount(x + 0, y - 1, z + 0, blockType);
                    byte a000 = GetLiquidAmount(x + 1, y + 0, z + 0, blockType);
                    byte a045 = GetLiquidAmount(x + 1, y + 0, z + 1, blockType);
                    byte a090 = GetLiquidAmount(x + 0, y + 0, z + 1, blockType);
                    byte a135 = GetLiquidAmount(x - 1, y + 0, z + 1, blockType);
                    byte a180 = GetLiquidAmount(x - 1, y + 0, z + 0, blockType);
                    byte a225 = GetLiquidAmount(x - 1, y + 0, z - 1, blockType);
                    byte a270 = GetLiquidAmount(x + 0, y + 0, z - 1, blockType);
                    byte a315 = GetLiquidAmount(x + 1, y + 0, z - 1, blockType);

                    bool s000 = IsSolid(x + 1, y + 0, z + 0);
                    bool s045 = IsSolid(x + 1, y + 0, z + 1);
                    bool s090 = IsSolid(x + 0, y + 0, z + 1);
                    bool s135 = IsSolid(x - 1, y + 0, z + 1);
                    bool s180 = IsSolid(x - 1, y + 0, z + 0);
                    bool s225 = IsSolid(x - 1, y + 0, z - 1);
                    bool s270 = IsSolid(x + 0, y + 0, z - 1);
                    bool s315 = IsSolid(x + 1, y + 0, z - 1);

					bool lq000 = IsLiquid(x + 1, y + 0, z + 0);
					bool lq045 = IsLiquid(x + 1, y + 0, z + 1);
					bool lq090 = IsLiquid(x + 0, y + 0, z + 1);
					bool lq135 = IsLiquid(x - 1, y + 0, z + 1);
					bool lq180 = IsLiquid(x - 1, y + 0, z + 0);
					bool lq225 = IsLiquid(x - 1, y + 0, z - 1);
					bool lq270 = IsLiquid(x + 0, y + 0, z - 1);
					bool lq315 = IsLiquid(x + 1, y + 0, z - 1);

					bool lqt000 = IsLiquid(x + 1, y + 1, z + 0);
					bool lqt045 = IsLiquid(x + 1, y + 1, z + 1);
					bool lqt090 = IsLiquid(x + 0, y + 1, z + 1);
					bool lqt135 = IsLiquid(x - 1, y + 1, z + 1);
					bool lqt180 = IsLiquid(x - 1, y + 1, z + 0);
					bool lqt225 = IsLiquid(x - 1, y + 1, z - 1);
					bool lqt270 = IsLiquid(x + 0, y + 1, z - 1);
					bool lqt315 = IsLiquid(x + 1, y + 1, z - 1);

					Func<int, int, int, bool> hasFace = null;
                    if (isLiquid) {
						hasFace = (int x, int y, int z) => {
							return GetBlock(x, y, z) != blockType
                                && (IsVoxelTransparent(x, y, z) || y - localBlockCoordinate.y == 1);
						};
					} else {
                        hasFace = (int x, int y, int z) => {
                            return IsVoxelTransparent(x, y, z) && GetBlock(x, y, z) != blockType;
                        };
                    }

                    // Right face.
                    if (hasFace(x + 1, y, z)) {
                        Vector2 atlasPosition = (Vector2)blockData.TexturingData.RightFace * atlasStep;

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

                        float dir = 0.0f;

						var uv1 = new Vector2(0.0f, 0.0f);
						var uv2 = new Vector2(0.0f, 1.0f);
						var uv3 = new Vector2(1.0f, 1.0f);
                        var uv4 = new Vector2(1.0f, 0.0f);

						if (isLiquid) {
                            byte afbk2 = aown == LiquidMap.MAX
                                || a000 == LiquidMap.MAX
                                || a270 == LiquidMap.MAX
                                || a315 == LiquidMap.MAX ? LiquidMap.MAX : LiquidMap.MIN;
							byte afbk3 = aown == LiquidMap.MAX
								|| a000 == LiquidMap.MAX
								|| a045 == LiquidMap.MAX
								|| a090 == LiquidMap.MAX ? LiquidMap.MAX : LiquidMap.MIN;
							if (atop == 0) {
                                h2 = lqt000 || lqt270 || lqt315 ? 1.0f : (aown
                                + (lq000 ? a000 : afbk2)
                                + (lq270 ? a270 : afbk2)
                                + (lq315 ? a315 : afbk2)) / 4.0f / (LiquidMap.MAX + 1);
                                h3 = lqt000 || lqt045 || lqt090 ? 1.0f : (aown
                                + (lq000 ? a000 : afbk3)
                                + (lq045 ? a045 : afbk3)
                                + (lq090 ? a090 : afbk3)) / 4.0f / (LiquidMap.MAX + 1);
                            }
                            if (a000 != 0) {
                                h1 = (aown
                                + (lq000 ? a000 : afbk2)
                                + (lq270 ? a270 : afbk2)
                                + (lq315 ? a315 : afbk2)) / 4.0f / (LiquidMap.MAX + 1);
                                h4 = (aown
                                + (lq000 ? a000 : afbk3)
                                + (lq045 ? a045 : afbk3)
                                + (lq090 ? a090 : afbk3)) / 4.0f / (LiquidMap.MAX + 1);
                            }

                            dir = 5.0f;

                            uv2.y = h2;
                            uv3.y = h3;
                        } else {
						    uv1 = new Vector2(atlasPosition.x + 0 * atlasStep, atlasPosition.y + h1 * atlasStep);
						    uv2 = new Vector2(atlasPosition.x + 0 * atlasStep, atlasPosition.y + h2 * atlasStep);
						    uv3 = new Vector2(atlasPosition.x + 1 * atlasStep, atlasPosition.y + h3 * atlasStep);
						    uv4 = new Vector2(atlasPosition.x + 1 * atlasStep, atlasPosition.y + h4 * atlasStep);
                        }

						float aof1 = lr1 + lg1 + lb1 + ls1;
                        float aof2 = lr2 + lg2 + lb2 + ls2;
                        float aof3 = lr3 + lg3 + lb3 + ls3;
                        float aof4 = lr4 + lg4 + lb4 + ls4;

#if CHUNK_UTILITY_PARALLEL_FOR
                        lock (lockObject) {
#endif
                        AddFaceIndices(meshData, aof1, aof2, aof3, aof4);
                        meshData.Vertices.Add(new Vertex(x + 1, y + h1, z + 0, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
                        meshData.Vertices.Add(new Vertex(x + 1, y + h2, z + 0, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
                        meshData.Vertices.Add(new Vertex(x + 1, y + h3, z + 1, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
                        meshData.Vertices.Add(new Vertex(x + 1, y + h4, z + 1, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));

                        if (isSolid) {
                            AddFaceColliderIndices(meshData);
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 1));
                        }
#if CHUNK_UTILITY_PARALLEL_FOR
					    }
#endif
				    }

                    // Left face.
                    if (hasFace(x - 1, y, z)) {
                        Vector2 atlasPosition = (Vector2)blockData.TexturingData.LeftFace * atlasStep;

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

						float dir = 0.0f;

						var uv1 = new Vector2(0.0f, 0.0f);
						var uv2 = new Vector2(0.0f, 1.0f);
						var uv3 = new Vector2(1.0f, 1.0f);
						var uv4 = new Vector2(1.0f, 0.0f);

						if (isLiquid) {
							byte afbk2 = aown == LiquidMap.MAX
								|| a090 == LiquidMap.MAX
								|| a135 == LiquidMap.MAX
								|| a090 == LiquidMap.MAX ? LiquidMap.MAX : LiquidMap.MIN;
							byte afbk3 = aown == LiquidMap.MAX
								|| a180 == LiquidMap.MAX
								|| a225 == LiquidMap.MAX
								|| a270 == LiquidMap.MAX ? LiquidMap.MAX : LiquidMap.MIN;
							if (atop == 0) {
                                h2 = lqt090 || lqt135 || lqt180 ? 1.0f : (aown
                                + (lq090 ? a090 : afbk2)
                                + (lq135 ? a135 : afbk2)
                                + (lq180 ? a180 : afbk2)) / 4.0f / (LiquidMap.MAX + 1);
                                h3 = lqt180 || lqt225 || lqt270 ? 1.0f :  (aown
                                + (lq180 ? a180 : afbk3)
                                + (lq225 ? a225 : afbk3)
                                + (lq270 ? a270 : afbk3)) / 4.0f / (LiquidMap.MAX + 1);
                            }
							if (a180 != 0) {
                                h1 = (aown
                                + (lq090 ? a090 : afbk2)
                                + (lq135 ? a135 : afbk2)
                                + (lq180 ? a180 : afbk2)) / 4.0f / (LiquidMap.MAX + 1);
                                h4 = (aown
                                + (lq180 ? a180 : afbk3)
                                + (lq225 ? a225 : afbk3)
                                + (lq270 ? a270 : afbk3)) / 4.0f / (LiquidMap.MAX + 1);
                            }

							dir = 5.0f;

							uv2.y = h2;
							uv3.y = h3;
						} else {
                            uv1 = new Vector2(atlasPosition.x + 0 * atlasStep, atlasPosition.y + h1 * atlasStep);
                            uv2 = new Vector2(atlasPosition.x + 0 * atlasStep, atlasPosition.y + h2 * atlasStep);
                            uv3 = new Vector2(atlasPosition.x + 1 * atlasStep, atlasPosition.y + h3 * atlasStep);
                            uv4 = new Vector2(atlasPosition.x + 1 * atlasStep, atlasPosition.y + h4 * atlasStep);
						}

                        float aof1 = lr1 + lg1 + lb1 + ls1;
                        float aof2 = lr2 + lg2 + lb2 + ls2;
                        float aof3 = lr3 + lg3 + lb3 + ls3;
                        float aof4 = lr4 + lg4 + lb4 + ls4;

#if CHUNK_UTILITY_PARALLEL_FOR
                        lock (lockObject) {
#endif
						AddFaceIndices(meshData, aof1, aof2, aof3, aof4);
                        meshData.Vertices.Add(new Vertex(x + 0, y + h1, z + 1, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
                        meshData.Vertices.Add(new Vertex(x + 0, y + h2, z + 1, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
                        meshData.Vertices.Add(new Vertex(x + 0, y + h3, z + 0, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
                        meshData.Vertices.Add(new Vertex(x + 0, y + h4, z + 0, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));

                        if (isSolid) {
                            AddFaceColliderIndices(meshData);
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 0));
                        }
#if CHUNK_UTILITY_PARALLEL_FOR
					    }
#endif
					}

					// Top face.
					if (hasFace(x, y + 1, z)) {
                        Vector2 atlasPosition = (Vector2)blockData.TexturingData.TopFace * atlasStep;

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

						float dir = 0.0f;

						var uv1 = new Vector2(0.0f, 0.0f);
						var uv2 = new Vector2(0.0f, 1.0f);
						var uv3 = new Vector2(1.0f, 1.0f);
						var uv4 = new Vector2(1.0f, 0.0f);

						if (isLiquid) {
							byte afbk1 = aown == LiquidMap.MAX
								|| a180 == LiquidMap.MAX
								|| a225 == LiquidMap.MAX
								|| a270 == LiquidMap.MAX ? LiquidMap.MAX : LiquidMap.MIN;
							byte afbk2 = aown == LiquidMap.MAX
								|| a090 == LiquidMap.MAX
								|| a135 == LiquidMap.MAX
								|| a180 == LiquidMap.MAX ? LiquidMap.MAX : LiquidMap.MIN;
							byte afbk3 = aown == LiquidMap.MAX
								|| a000 == LiquidMap.MAX
								|| a045 == LiquidMap.MAX
								|| a090 == LiquidMap.MAX ? LiquidMap.MAX : LiquidMap.MIN;
							byte afbk4 = aown == LiquidMap.MAX
								|| a000 == LiquidMap.MAX
								|| a270 == LiquidMap.MAX
								|| a315 == LiquidMap.MAX ? LiquidMap.MAX : LiquidMap.MIN;
							if (atop == 0) {
                                h1 = lqt180 || lqt225 || lqt270 ? 1.0f : (aown
                                + (lq180 ? a180 : afbk1)
                                + (lq225 ? a225 : afbk1)
                                + (lq270 ? a270 : afbk1)) / 4.0f / (LiquidMap.MAX + 1);
                                h2 = lqt090 || lqt135 || lqt180 ? 1.0f : (aown
                                + (lq090 ? a090 : afbk2)
                                + (lq135 ? a135 : afbk2)
                                + (lq180 ? a180 : afbk2)) / 4.0f / (LiquidMap.MAX + 1);
                                h3 = lqt000 || lqt045 || lqt090 ? 1.0f : (aown
                                + (lq000 ? a000 : afbk3)
                                + (lq045 ? a045 : afbk3)
                                + (lq090 ? a090 : afbk3)) / 4.0f / (LiquidMap.MAX + 1);
                                h4 = lqt000 || lqt270 || lqt315 ? 1.0f : (aown
                                + (lq000 ? a000 : afbk4)
                                + (lq270 ? a270 : afbk4)
                                + (lq315 ? a315 : afbk4)) / 4.0f / (LiquidMap.MAX + 1);

                                byte la000;
                                byte la045;
                                byte la090;
                                byte la135;
                                byte la180;
                                byte la225;
                                byte la270;
                                byte la315;

                                if (aown == LiquidMap.MAX) {
                                     la000 = s000 ? LiquidMap.MAX : a000; 
                                     la045 = s045 ? LiquidMap.MAX : a045; 
                                     la090 = s090 ? LiquidMap.MAX : a090; 
                                     la135 = s135 ? LiquidMap.MAX : a135; 
                                     la180 = s180 ? LiquidMap.MAX : a180; 
                                     la225 = s225 ? LiquidMap.MAX : a225; 
                                     la270 = s270 ? LiquidMap.MAX : a270;
								     la315 = s315 ? LiquidMap.MAX : a315;
                                } else {
                                    la000 = a000;
                                    la045 = a045;
                                    la090 = a090;
                                    la135 = a135;
                                    la180 = a180;
                                    la225 = a225;
                                    la270 = a270;
                                    la315 = a315;
                                }

                                if (la000 == la180 && la090 == la270 && aown == la000 && aown == la090) {
                                    dir = 0.0f;
                                } else if (la000 != la180 && la090 == la270) {
                                    if (la000 < la180 || la000 < aown || aown < la180)
                                        dir = 3.0f;
                                    else
                                        dir = 7.0f;
                                } else if (la090 != la270 && la000 == la180) {
                                    if (la090 < la270 || la090 < aown || aown < la270)
                                        dir = 1.0f;
                                    else
                                        dir = 5.0f;
                                } else if (la000 == la090 || la180 == la270) {
                                    if (la045 < la225 || la045 < aown || aown < la225)
                                        dir = 2.0f;
                                    else
                                        dir = 6.0f;
                                } else if (la090 == la180 || la000 == la270) {
                                    if (la315 < la135 || la315 < aown || aown < la135)
                                        dir = 4.0f;
                                    else
                                        dir = 8.0f;
                                }
							}
                        } else {
							uv1 = new Vector2(atlasPosition.x + 0 * atlasStep, atlasPosition.y + 0 * atlasStep);
							uv2 = new Vector2(atlasPosition.x + 0 * atlasStep, atlasPosition.y + 1 * atlasStep);
							uv3 = new Vector2(atlasPosition.x + 1 * atlasStep, atlasPosition.y + 1 * atlasStep);
							uv4 = new Vector2(atlasPosition.x + 1 * atlasStep, atlasPosition.y + 0 * atlasStep);
						}

                        float aof1 = lr1 + lg1 + lb1 + ls1;
                        float aof2 = lr2 + lg2 + lb2 + ls2;
                        float aof3 = lr3 + lg3 + lb3 + ls3;
                        float aof4 = lr4 + lg4 + lb4 + ls4;

#if CHUNK_UTILITY_PARALLEL_FOR
                        lock (lockObject) {
#endif
					    bool fliped = isLiquid && (lqt045 || lqt225);
                        AddFaceIndices(meshData, aof1, aof2, aof3, aof4, true, fliped);
                        meshData.Vertices.Add(new Vertex(x + 0, y + 1 * h1, z + 0, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 1 * h2, z + 1, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 1 * h3, z + 1, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 1 * h4, z + 0, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));

                        if (isSolid) {
                            AddFaceColliderIndices(meshData);
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 0));
                        }
#if CHUNK_UTILITY_PARALLEL_FOR
					    }
#endif
					}

					// Bottom face.
					if (hasFace(x, y - 1, z)) {
                        Vector2 atlasPosition = (Vector2)blockData.TexturingData.BottomFace * atlasStep;

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

						float dir1 = 0.0f;
						float dir2 = 0.0f;
						float dir3 = 0.0f;
						float dir4 = 0.0f;

						var uv1 = new Vector2(0.0f, 0.0f);
						var uv2 = new Vector2(0.0f, 1.0f);
						var uv3 = new Vector2(1.0f, 1.0f);
						var uv4 = new Vector2(1.0f, 0.0f);

                        if (!isLiquid) {
							uv1 = new Vector2(atlasPosition.x + 0 * atlasStep, atlasPosition.y + 0 * atlasStep);
							uv2 = new Vector2(atlasPosition.x + 0 * atlasStep, atlasPosition.y + 1 * atlasStep);
							uv3 = new Vector2(atlasPosition.x + 1 * atlasStep, atlasPosition.y + 1 * atlasStep);
							uv4 = new Vector2(atlasPosition.x + 1 * atlasStep, atlasPosition.y + 0 * atlasStep);
						}

#if CHUNK_UTILITY_PARALLEL_FOR
                        lock (lockObject) {
#endif
				        AddFaceIndices(meshData, aof1, aof2, aof3, aof4);
                        meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir1));
                        meshData.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir2));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir3));
                        meshData.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir4));

                        if (isSolid) {
                            AddFaceColliderIndices(meshData);
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 0));
                        }
#if CHUNK_UTILITY_PARALLEL_FOR
					    }
#endif
					}

					// Front face.
					if (hasFace(x, y, z + 1)) {
                        Vector2 atlasPosition = (Vector2)blockData.TexturingData.BackFace * atlasStep;

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

                        float dir = 0.0f;

						var uv1 = new Vector2(0.0f, 0.0f);
						var uv2 = new Vector2(0.0f, 1.0f);
						var uv3 = new Vector2(1.0f, 1.0f);
						var uv4 = new Vector2(1.0f, 0.0f);

						if (isLiquid) {
							byte afbk2 = aown == LiquidMap.MAX
								|| a000 == LiquidMap.MAX
								|| a045 == LiquidMap.MAX
								|| a090 == LiquidMap.MAX ? LiquidMap.MAX : LiquidMap.MIN;
							byte afbk3 = aown == LiquidMap.MAX
								|| a090 == LiquidMap.MAX
								|| a135 == LiquidMap.MAX
								|| a180 == LiquidMap.MAX ? LiquidMap.MAX : LiquidMap.MIN;
							if (atop == 0) {
                                h2 = lqt000 || lqt045 || lqt090 ? 1.0f : (aown
                                + (lq000 ? a000 : afbk2)
                                + (lq045 ? a045 : afbk2)
                                + (lq090 ? a090 : afbk2)) / 4.0f / (LiquidMap.MAX + 1);
                                h3 = lqt090 || lqt135 || lqt180 ? 1.0f : (aown
                                + (lq090 ? a090 : afbk3)
                                + (lq135 ? a135 : afbk3)
                                + (lq180 ? a180 : afbk3)) / 4.0f / (LiquidMap.MAX + 1);
                            }
                            if (a090 != 0) {
                                h1 = (aown
                                + (lq000 ? a000 : afbk2)
                                + (lq045 ? a045 : afbk2)
                                + (lq090 ? a090 : afbk2)) / 4.0f / (LiquidMap.MAX + 1);
                                h4 = (aown
                                + (lq090 ? a090 : afbk3)
                                + (lq135 ? a135 : afbk3)
                                + (lq180 ? a180 : afbk3)) / 4.0f / (LiquidMap.MAX + 1);
                            }

							dir = 5.0f;

							uv2.y = h2;
							uv3.y = h3;
						} else {
							uv1 = new Vector2(atlasPosition.x + 0 * atlasStep, atlasPosition.y + h1 * atlasStep);
							uv2 = new Vector2(atlasPosition.x + 0 * atlasStep, atlasPosition.y + h2 * atlasStep);
							uv3 = new Vector2(atlasPosition.x + 1 * atlasStep, atlasPosition.y + h3 * atlasStep);
							uv4 = new Vector2(atlasPosition.x + 1 * atlasStep, atlasPosition.y + h4 * atlasStep);
						}

                        float aof1 = lr1 + lg1 + lb1 + ls1;
                        float aof2 = lr2 + lg2 + lb2 + ls2;
                        float aof3 = lr3 + lg3 + lb3 + ls3;
                        float aof4 = lr4 + lg4 + lb4 + ls4;

#if CHUNK_UTILITY_PARALLEL_FOR
                        lock (lockObject) {
#endif
                        AddFaceIndices(meshData, aof1, aof2, aof3, aof4);
                        meshData.Vertices.Add(new Vertex(x + 1, y + h1, z + 1, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
                        meshData.Vertices.Add(new Vertex(x + 1, y + h2, z + 1, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
                        meshData.Vertices.Add(new Vertex(x + 0, y + h3, z + 1, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
                        meshData.Vertices.Add(new Vertex(x + 0, y + h4, z + 1, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));

                        if (isSolid) {
                            AddFaceColliderIndices(meshData);
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 1));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 1));
                        }
#if CHUNK_UTILITY_PARALLEL_FOR
					    }
#endif
					}

					// Back face.
					if (hasFace(x, y, z - 1)) {
                        Vector2 atlasPosition = (Vector2)blockData.TexturingData.FrontFace * atlasStep;

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

						float dir = 0.0f;

						var uv1 = new Vector2(0.0f, 0.0f);
						var uv2 = new Vector2(0.0f, 1.0f);
						var uv3 = new Vector2(1.0f, 1.0f);
						var uv4 = new Vector2(1.0f, 0.0f);

						if (isLiquid) {
							byte afbk2 = aown == LiquidMap.MAX
								|| a180 == LiquidMap.MAX
								|| a225 == LiquidMap.MAX
								|| a270 == LiquidMap.MAX ? LiquidMap.MAX : LiquidMap.MIN;
							byte afbk3 = aown == LiquidMap.MAX
								|| a000 == LiquidMap.MAX
								|| a270 == LiquidMap.MAX
								|| a315 == LiquidMap.MAX ? LiquidMap.MAX : LiquidMap.MIN;
							if (atop == 0) {
                                h2 = lqt180 || lqt225 || lqt270 ? 1.0f : (aown
                                + (lq180 ? a180 : afbk2)
                                + (lq225 ? a225 : afbk2)
                                + (lq270 ? a270 : afbk2)) / 4.0f / (LiquidMap.MAX + 1);
                                h3 = lqt000 || lqt270 || lqt315 ? 1.0f : (aown
                                + (lq000 ? a000 : afbk3)
                                + (lq270 ? a270 : afbk3)
                                + (lq315 ? a315 : afbk3)) / 4.0f / (LiquidMap.MAX + 1);
                            }
                            if (a270 != 0) {
                                h1 = (aown
                                + (lq180 ? a180 : afbk2)
                                + (lq225 ? a225 : afbk2)
                                + (lq270 ? a270 : afbk2)) / 4.0f / (LiquidMap.MAX + 1);
                                h4 = (aown
                                + (lq000 ? a000 : afbk3)
                                + (lq270 ? a270 : afbk3)
                                + (lq315 ? a315 : afbk3)) / 4.0f / (LiquidMap.MAX + 1);
                            }

							dir = 5.0f;

							uv2.y = h2;
							uv3.y = h3;
						} else {
							uv1 = new Vector2(atlasPosition.x + 0 * atlasStep, atlasPosition.y + h1 * atlasStep);
							uv2 = new Vector2(atlasPosition.x + 0 * atlasStep, atlasPosition.y + h2 * atlasStep);
							uv3 = new Vector2(atlasPosition.x + 1 * atlasStep, atlasPosition.y + h3 * atlasStep);
							uv4 = new Vector2(atlasPosition.x + 1 * atlasStep, atlasPosition.y + h4 * atlasStep);
						}

                        float aof1 = lr1 + lg1 + lb1 + ls1;
                        float aof2 = lr2 + lg2 + lb2 + ls2;
                        float aof3 = lr3 + lg3 + lb3 + ls3;
                        float aof4 = lr4 + lg4 + lb4 + ls4;

#if CHUNK_UTILITY_PARALLEL_FOR
                        lock (lockObject) {
#endif
	                    AddFaceIndices(meshData, aof1, aof2, aof3, aof4);
                        meshData.Vertices.Add(new Vertex(x + 0, y + h1, z + 0, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
                        meshData.Vertices.Add(new Vertex(x + 0, y + h2, z + 0, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
                        meshData.Vertices.Add(new Vertex(x + 1, y + h3, z + 0, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
                        meshData.Vertices.Add(new Vertex(x + 1, y + h4, z + 0, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));

                        if (isSolid) {
                            AddFaceColliderIndices(meshData);
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 0, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 0, y + 1, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 1, z + 0));
                            meshData.ColliderVertices.Add(new Vector3(x + 1, y + 0, z + 0));
                        }
#if CHUNK_UTILITY_PARALLEL_FOR
					    }
#endif
					}
				}
            });

            return result;
        }
    }
}