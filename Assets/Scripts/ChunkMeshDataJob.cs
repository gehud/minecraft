using Minecraft.Lighting;
using Minecraft.Utilities;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Minecraft {
    [BurstCompile]
    public struct ChunkMeshDataJob : IJob, IDisposable {
        [ReadOnly]
        public Entity Entity;
        public NativeList<Vertex> Vertices;
        public NativeList<ushort> OpaqueIndices;
        public NativeList<ushort> TransparentIndices;
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<NativeArray<Voxel>> Claster;
        [ReadOnly]
        public int3 ChunkCoordinate;
        [ReadOnly]
        public NativeArray<Block> Blocks;

        public void Execute() {
            for (int x = 0; x < Chunk.Size; x++) {
                for (int y = 0; y < Chunk.Size; y++) {
                    for (int z = 0; z < Chunk.Size; z++) {
                        var localVoxelCoordinate = new int3(x, y, z);
                        GetVoxel(localVoxelCoordinate, out Voxel voxel);

                        if (voxel.Type == BlockType.Air) {
                            continue;
                        }

                        var block = Blocks[(int)voxel.Type];
                        var texturing = block.Texturing;
                        var isTransparent = block.IsTransparent;
                        var indices = isTransparent ? TransparentIndices : OpaqueIndices;

                        // Right face
                        if (HasFace(localVoxelCoordinate + new int3(1, 0, 0), voxel.Type)) {
                            var faceTexturing = texturing.Right;
                            var u1 = faceTexturing.x;
                            var u2 = faceTexturing.x;
                            var u3 = faceTexturing.x + 1;
                            var u4 = faceTexturing.x + 1;
                            var v1 = faceTexturing.y;
                            var v2 = faceTexturing.y + 1;
                            var v3 = faceTexturing.y + 1;
                            var v4 = faceTexturing.y;

                            bool t000 = !IsTransparent(x + 1, y + 0, z + 1);
                            bool t090 = !IsTransparent(x + 1, y + 1, z + 0);
                            bool t180 = !IsTransparent(x + 1, y + 0, z - 1);
                            bool t270 = !IsTransparent(x + 1, y - 1, z + 0);

                            var lrtop = GetLight(x + 1, y + 0, z + 0, LightChanel.Red);
                            var lr000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Red);
                            var lr045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Red);
                            var lr090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Red);
                            var lr135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Red);
                            var lr180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Red);
                            var lr225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Red);
                            var lr270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Red);
                            var lr315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Red);

                            var lr1 = t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270;
                            var lr2 = t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180;
                            var lr3 = t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090;
                            var lr4 = t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315;

                            var lgtop = GetLight(x + 1, y + 0, z + 0, LightChanel.Green);
                            var lg000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Green);
                            var lg045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Green);
                            var lg090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Green);
                            var lg135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Green);
                            var lg180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Green);
                            var lg225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Green);
                            var lg270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Green);
                            var lg315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Green);

                            var lg1 = t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270;
                            var lg2 = t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180;
                            var lg3 = t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090;
                            var lg4 = t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315;

                            var lbtop = GetLight(x + 1, y + 0, z + 0, LightChanel.Blue);
                            var lb000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Blue);
                            var lb045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Blue);
                            var lb090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Blue);
                            var lb135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Blue);
                            var lb180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Blue);
                            var lb225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Blue);
                            var lb270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Blue);
                            var lb315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Blue);

                            var lb1 = t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270;
                            var lb2 = t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180;
                            var lb3 = t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090;
                            var lb4 = t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315;

                            var lstop = GetLight(x + 1, y + 0, z + 0, LightChanel.Sun);
                            var ls000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Sun);
                            var ls045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Sun);
                            var ls090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Sun);
                            var ls135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Sun);
                            var ls180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Sun);
                            var ls225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Sun);
                            var ls270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Sun);
                            var ls315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Sun);

                            var ls1 = t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270;
                            var ls2 = t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180;
                            var ls3 = t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090;
                            var ls4 = t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315;

                            var aof1 = lr1 + lg1 + lb1 + ls1;
                            var aof2 = lr2 + lg2 + lb2 + ls2;
                            var aof3 = lr3 + lg3 + lb3 + ls3;
                            var aof4 = lr4 + lg4 + lb4 + ls4;

                            AddFaceIndices(indices, aof1, aof2, aof3, aof4);
                            Vertices.Add(Pack(x + 1, y + 0, z + 0, u1, v1, lr1, lg1, lb1, ls1));
                            Vertices.Add(Pack(x + 1, y + 1, z + 0, u2, v2, lr2, lg2, lb2, ls2));
                            Vertices.Add(Pack(x + 1, y + 1, z + 1, u3, v3, lr3, lg3, lb3, ls3));
                            Vertices.Add(Pack(x + 1, y + 0, z + 1, u4, v4, lr4, lg4, lb4, ls4));
                        }

                        // Left face
                        if (HasFace(localVoxelCoordinate + new int3(-1, 0, 0), voxel.Type)) {
                            var faceTexturing = texturing.Left;
                            var u1 = faceTexturing.x;
                            var u2 = faceTexturing.x;
                            var u3 = faceTexturing.x + 1;
                            var u4 = faceTexturing.x + 1;
                            var v1 = faceTexturing.y;
                            var v2 = faceTexturing.y + 1;
                            var v3 = faceTexturing.y + 1;
                            var v4 = faceTexturing.y;
                            ;

                            bool t000 = !IsTransparent(x - 1, y + 0, z - 1);
                            bool t090 = !IsTransparent(x - 1, y + 1, z + 0);
                            bool t180 = !IsTransparent(x - 1, y + 0, z + 1);
                            bool t270 = !IsTransparent(x - 1, y - 1, z + 0);

                            var lrtop = GetLight(x - 1, y + 0, z + 0, LightChanel.Red);
                            var lr000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Red);
                            var lr045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Red);
                            var lr090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Red);
                            var lr135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Red);
                            var lr180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Red);
                            var lr225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Red);
                            var lr270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Red);
                            var lr315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Red);

                            var lr1 = t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270;
                            var lr2 = t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180;
                            var lr3 = t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090;
                            var lr4 = t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315;

                            var lgtop = GetLight(x - 1, y + 0, z + 0, LightChanel.Green);
                            var lg000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Green);
                            var lg045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Green);
                            var lg090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Green);
                            var lg135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Green);
                            var lg180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Green);
                            var lg225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Green);
                            var lg270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Green);
                            var lg315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Green);

                            var lg1 = t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270;
                            var lg2 = t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180;
                            var lg3 = t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090;
                            var lg4 = t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315;

                            var lbtop = GetLight(x - 1, y + 0, z + 0, LightChanel.Blue);
                            var lb000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Blue);
                            var lb045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Blue);
                            var lb090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Blue);
                            var lb135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Blue);
                            var lb180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Blue);
                            var lb225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Blue);
                            var lb270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Blue);
                            var lb315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Blue);

                            var lb1 = t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270;
                            var lb2 = t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180;
                            var lb3 = t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090;
                            var lb4 = t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315;

                            var lstop = GetLight(x - 1, y + 0, z + 0, LightChanel.Sun);
                            var ls000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Sun);
                            var ls045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Sun);
                            var ls090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Sun);
                            var ls135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Sun);
                            var ls180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Sun);
                            var ls225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Sun);
                            var ls270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Sun);
                            var ls315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Sun);

                            var ls1 = t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270;
                            var ls2 = t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180;
                            var ls3 = t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090;
                            var ls4 = t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315;

                            var aof1 = lr1 + lg1 + lb1 + ls1;
                            var aof2 = lr2 + lg2 + lb2 + ls2;
                            var aof3 = lr3 + lg3 + lb3 + ls3;
                            var aof4 = lr4 + lg4 + lb4 + ls4;

                            AddFaceIndices(indices, aof1, aof2, aof3, aof4);
                            Vertices.Add(Pack(x + 0, y + 0, z + 1, u1, v1, lr1, lg1, lb1, ls1));
                            Vertices.Add(Pack(x + 0, y + 1, z + 1, u2, v2, lr2, lg2, lb2, ls2));
                            Vertices.Add(Pack(x + 0, y + 1, z + 0, u3, v3, lr3, lg3, lb3, ls3));
                            Vertices.Add(Pack(x + 0, y + 0, z + 0, u4, v4, lr4, lg4, lb4, ls4));
                        }

                        // Top face
                        if (HasFace(localVoxelCoordinate + new int3(0, 1, 0), voxel.Type)) {
                            var faceTexturing = texturing.Top;
                            var u1 = faceTexturing.x;
                            var u2 = faceTexturing.x;
                            var u3 = faceTexturing.x + 1;
                            var u4 = faceTexturing.x + 1;
                            var v1 = faceTexturing.y;
                            var v2 = faceTexturing.y + 1;
                            var v3 = faceTexturing.y + 1;
                            var v4 = faceTexturing.y;
                            ;

                            bool t000 = !IsTransparent(x + 1, y + 1, z + 0);
                            bool t090 = !IsTransparent(x + 0, y + 1, z + 1);
                            bool t180 = !IsTransparent(x - 1, y + 1, z + 0);
                            bool t270 = !IsTransparent(x + 0, y + 1, z - 1);

                            var lrtop = GetLight(x + 0, y + 1, z + 0, LightChanel.Red);
                            var lr000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Red);
                            var lr045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Red);
                            var lr090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Red);
                            var lr135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Red);
                            var lr180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Red);
                            var lr225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Red);
                            var lr270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Red);
                            var lr315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Red);

                            var lr1 = t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270;
                            var lr2 = t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180;
                            var lr3 = t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090;
                            var lr4 = t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315;

                            var lgtop = GetLight(x + 0, y + 1, z + 0, LightChanel.Green);
                            var lg000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Green);
                            var lg045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Green);
                            var lg090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Green);
                            var lg135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Green);
                            var lg180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Green);
                            var lg225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Green);
                            var lg270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Green);
                            var lg315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Green);

                            var lg1 = t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270;
                            var lg2 = t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180;
                            var lg3 = t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090;
                            var lg4 = t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315;

                            var lbtop = GetLight(x + 0, y + 1, z + 0, LightChanel.Blue);
                            var lb000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Blue);
                            var lb045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Blue);
                            var lb090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Blue);
                            var lb135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Blue);
                            var lb180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Blue);
                            var lb225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Blue);
                            var lb270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Blue);
                            var lb315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Blue);

                            var lb1 = t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270;
                            var lb2 = t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180;
                            var lb3 = t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090;
                            var lb4 = t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315;

                            var lstop = GetLight(x + 0, y + 1, z + 0, LightChanel.Sun);
                            var ls000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Sun);
                            var ls045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Sun);
                            var ls090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Sun);
                            var ls135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Sun);
                            var ls180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Sun);
                            var ls225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Sun);
                            var ls270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Sun);
                            var ls315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Sun);

                            var ls1 = t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270;
                            var ls2 = t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180;
                            var ls3 = t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090;
                            var ls4 = t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315;

                            var aof1 = lr1 + lg1 + lb1 + ls1;
                            var aof2 = lr2 + lg2 + lb2 + ls2;
                            var aof3 = lr3 + lg3 + lb3 + ls3;
                            var aof4 = lr4 + lg4 + lb4 + ls4;

                            AddFaceIndices(indices, aof1, aof2, aof3, aof4);
                            Vertices.Add(Pack(x + 0, y + 1, z + 0, u1, v1, lr1, lg1, lb1, ls1));
                            Vertices.Add(Pack(x + 0, y + 1, z + 1, u2, v2, lr2, lg2, lb2, ls2));
                            Vertices.Add(Pack(x + 1, y + 1, z + 1, u3, v3, lr3, lg3, lb3, ls3));
                            Vertices.Add(Pack(x + 1, y + 1, z + 0, u4, v4, lr4, lg4, lb4, ls4));
                        }

                        // Buttom face
                        if (HasFace(localVoxelCoordinate + new int3(0, -1, 0), voxel.Type)) {
                            var faceTexturing = texturing.Bottom;
                            var u1 = faceTexturing.x;
                            var u2 = faceTexturing.x;
                            var u3 = faceTexturing.x + 1;
                            var u4 = faceTexturing.x + 1;
                            var v1 = faceTexturing.y;
                            var v2 = faceTexturing.y + 1;
                            var v3 = faceTexturing.y + 1;
                            var v4 = faceTexturing.y;
                            ;

                            bool t000 = !IsTransparent(x - 1, y - 1, z + 0);
                            bool t090 = !IsTransparent(x + 0, y - 1, z + 1);
                            bool t180 = !IsTransparent(x + 1, y - 1, z + 0);
                            bool t270 = !IsTransparent(x + 0, y - 1, z - 1);

                            var lrtop = GetLight(x + 0, y - 1, z + 0, LightChanel.Red);
                            var lr000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Red);
                            var lr045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Red);
                            var lr090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Red);
                            var lr135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Red);
                            var lr180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Red);
                            var lr225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Red);
                            var lr270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Red);
                            var lr315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Red);

                            var lr1 = t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270;
                            var lr2 = t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180;
                            var lr3 = t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090;
                            var lr4 = t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315;

                            var lgtop = GetLight(x + 0, y - 1, z + 0, LightChanel.Green);
                            var lg000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Green);
                            var lg045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Green);
                            var lg090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Green);
                            var lg135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Green);
                            var lg180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Green);
                            var lg225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Green);
                            var lg270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Green);
                            var lg315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Green);

                            var lg1 = t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270;
                            var lg2 = t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180;
                            var lg3 = t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090;
                            var lg4 = t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315;

                            var lbtop = GetLight(x + 0, y - 1, z + 0, LightChanel.Blue);
                            var lb000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Blue);
                            var lb045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Blue);
                            var lb090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Blue);
                            var lb135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Blue);
                            var lb180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Blue);
                            var lb225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Blue);
                            var lb270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Blue);
                            var lb315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Blue);

                            var lb1 = t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270;
                            var lb2 = t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180;
                            var lb3 = t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090;
                            var lb4 = t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315;

                            var lstop = GetLight(x + 0, y - 1, z + 0, LightChanel.Sun);
                            var ls000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Sun);
                            var ls045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Sun);
                            var ls090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Sun);
                            var ls135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Sun);
                            var ls180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Sun);
                            var ls225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Sun);
                            var ls270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Sun);
                            var ls315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Sun);

                            var ls1 = t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270;
                            var ls2 = t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180;
                            var ls3 = t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090;
                            var ls4 = t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315;

                            var aof1 = lr1 + lg1 + lb1 + ls1;
                            var aof2 = lr2 + lg2 + lb2 + ls2;
                            var aof3 = lr3 + lg3 + lb3 + ls3;
                            var aof4 = lr4 + lg4 + lb4 + ls4;

                            AddFaceIndices(indices, aof1, aof2, aof3, aof4);
                            Vertices.Add(Pack(x + 1, y + 0, z + 0, u1, v1, lr1, lg1, lb1, ls1));
                            Vertices.Add(Pack(x + 1, y + 0, z + 1, u2, v2, lr2, lg2, lb2, ls2));
                            Vertices.Add(Pack(x + 0, y + 0, z + 1, u3, v3, lr3, lg3, lb3, ls3));
                            Vertices.Add(Pack(x + 0, y + 0, z + 0, u4, v4, lr4, lg4, lb4, ls4));
                        }

                        // Front face
                        if (HasFace(localVoxelCoordinate + new int3(0, 0, 1), voxel.Type)) {
                            var faceTexturing = texturing.Front;
                            var u1 = faceTexturing.x;
                            var u2 = faceTexturing.x;
                            var u3 = faceTexturing.x + 1;
                            var u4 = faceTexturing.x + 1;
                            var v1 = faceTexturing.y;
                            var v2 = faceTexturing.y + 1;
                            var v3 = faceTexturing.y + 1;
                            var v4 = faceTexturing.y;

                            bool t000 = !IsTransparent(x - 1, y + 0, z + 1);
                            bool t090 = !IsTransparent(x + 0, y + 1, z + 1);
                            bool t180 = !IsTransparent(x + 1, y + 0, z + 1);
                            bool t270 = !IsTransparent(x + 0, y - 1, z + 1);

                            var lrtop = GetLight(x + 0, y + 0, z + 1, LightChanel.Red);
                            var lr000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Red);
                            var lr045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Red);
                            var lr090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Red);
                            var lr135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Red);
                            var lr180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Red);
                            var lr225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Red);
                            var lr270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Red);
                            var lr315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Red);

                            var lr1 = t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270;
                            var lr2 = t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180;
                            var lr3 = t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090;
                            var lr4 = t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315;

                            var lgtop = GetLight(x + 0, y + 0, z + 1, LightChanel.Green);
                            var lg000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Green);
                            var lg045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Green);
                            var lg090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Green);
                            var lg135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Green);
                            var lg180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Green);
                            var lg225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Green);
                            var lg270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Green);
                            var lg315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Green);

                            var lg1 = t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270;
                            var lg2 = t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180;
                            var lg3 = t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090;
                            var lg4 = t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315;

                            var lbtop = GetLight(x + 0, y + 0, z + 1, LightChanel.Blue);
                            var lb000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Blue);
                            var lb045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Blue);
                            var lb090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Blue);
                            var lb135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Blue);
                            var lb180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Blue);
                            var lb225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Blue);
                            var lb270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Blue);
                            var lb315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Blue);

                            var lb1 = t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270;
                            var lb2 = t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180;
                            var lb3 = t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090;
                            var lb4 = t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315;

                            var lstop = GetLight(x + 0, y + 0, z + 1, LightChanel.Sun);
                            var ls000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Sun);
                            var ls045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Sun);
                            var ls090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Sun);
                            var ls135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Sun);
                            var ls180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Sun);
                            var ls225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Sun);
                            var ls270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Sun);
                            var ls315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Sun);

                            var ls1 = t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270;
                            var ls2 = t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180;
                            var ls3 = t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090;
                            var ls4 = t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315;

                            var aof1 = lr1 + lg1 + lb1 + ls1;
                            var aof2 = lr2 + lg2 + lb2 + ls2;
                            var aof3 = lr3 + lg3 + lb3 + ls3;
                            var aof4 = lr4 + lg4 + lb4 + ls4;

                            AddFaceIndices(indices, aof1, aof2, aof3, aof4);
                            Vertices.Add(Pack(x + 1, y + 0, z + 1, u1, v1, lr1, lg1, lb1, ls1));
                            Vertices.Add(Pack(x + 1, y + 1, z + 1, u2, v2, lr2, lg2, lb2, ls2));
                            Vertices.Add(Pack(x + 0, y + 1, z + 1, u3, v3, lr3, lg3, lb3, ls3));
                            Vertices.Add(Pack(x + 0, y + 0, z + 1, u4, v4, lr4, lg4, lb4, ls4));
                        }

                        // Back face
                        if (HasFace(localVoxelCoordinate + new int3(0, 0, -1), voxel.Type)) {
                            var faceTexturing = texturing.Back;
                            var u1 = faceTexturing.x;
                            var u2 = faceTexturing.x;
                            var u3 = faceTexturing.x + 1;
                            var u4 = faceTexturing.x + 1;
                            var v1 = faceTexturing.y;
                            var v2 = faceTexturing.y + 1;
                            var v3 = faceTexturing.y + 1;
                            var v4 = faceTexturing.y;

                            bool t000 = !IsTransparent(x + 1, y + 0, z - 1);
                            bool t090 = !IsTransparent(x + 0, y + 1, z - 1);
                            bool t180 = !IsTransparent(x - 1, y + 0, z - 1);
                            bool t270 = !IsTransparent(x + 0, y - 1, z - 1);

                            var lrtop = GetLight(x + 0, y + 0, z - 1, LightChanel.Red);
                            var lr000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Red);
                            var lr045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Red);
                            var lr090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Red);
                            var lr135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Red);
                            var lr180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Red);
                            var lr225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Red);
                            var lr270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Red);
                            var lr315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Red);

                            var lr1 = t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270;
                            var lr2 = t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180;
                            var lr3 = t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090;
                            var lr4 = t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315;

                            var lgtop = GetLight(x + 0, y + 0, z - 1, LightChanel.Green);
                            var lg000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Green);
                            var lg045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Green);
                            var lg090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Green);
                            var lg135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Green);
                            var lg180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Green);
                            var lg225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Green);
                            var lg270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Green);
                            var lg315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Green);

                            var lg1 = t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270;
                            var lg2 = t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180;
                            var lg3 = t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090;
                            var lg4 = t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315;

                            var lbtop = GetLight(x + 0, y + 0, z - 1, LightChanel.Blue);
                            var lb000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Blue);
                            var lb045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Blue);
                            var lb090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Blue);
                            var lb135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Blue);
                            var lb180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Blue);
                            var lb225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Blue);
                            var lb270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Blue);
                            var lb315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Blue);

                            var lb1 = t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270;
                            var lb2 = t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180;
                            var lb3 = t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090;
                            var lb4 = t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315;

                            var lstop = GetLight(x + 0, y + 0, z - 1, LightChanel.Sun);
                            var ls000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Sun);
                            var ls045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Sun);
                            var ls090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Sun);
                            var ls135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Sun);
                            var ls180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Sun);
                            var ls225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Sun);
                            var ls270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Sun);
                            var ls315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Sun);

                            var ls1 = t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270;
                            var ls2 = t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180;
                            var ls3 = t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090;
                            var ls4 = t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315;

                            var aof1 = lr1 + lg1 + lb1 + ls1;
                            var aof2 = lr2 + lg2 + lb2 + ls2;
                            var aof3 = lr3 + lg3 + lb3 + ls3;
                            var aof4 = lr4 + lg4 + lb4 + ls4;

                            AddFaceIndices(indices, aof1, aof2, aof3, aof4);
                            Vertices.Add(Pack(x + 0, y + 0, z + 0, u1, v1, lr1, lg1, lb1, ls1));
                            Vertices.Add(Pack(x + 0, y + 1, z + 0, u2, v2, lr2, lg2, lb2, ls2));
                            Vertices.Add(Pack(x + 1, y + 1, z + 0, u3, v3, lr3, lg3, lb3, ls3));
                            Vertices.Add(Pack(x + 1, y + 0, z + 0, u4, v4, lr4, lg4, lb4, ls4));
                        }
                    }
                }
            }
        }

        private Vertex Pack(int x, int y, int z, int u, int v, int r, int g, int b, int s) {
            // Layout:
            // A
            // x - 5bit, y - 5bit, z - 5bit, i - 9bit
            // B
            // r - 6bit, g - 6bit, b - 6bit, s - 6bit 

            var i = IndexUtility.ToIndex(u, v, 17);

            // A
            var yBit = 5;
            var zBit = 5;
            var iBit = 9;

            var ziBit = zBit + iBit;
            var yziBit = yBit + ziBit;

            // B
            var gBit = 6;
            var bBit = 6;
            var sBit = 6;

            var bsBit = bBit + sBit;
            var gbsBit = gBit + bsBit;

            return new Vertex {
                A = (uint)(x << yziBit | y << ziBit | z << iBit | i),
                B = (uint)(r << gbsBit | g << bsBit | b << sBit | s)
            };
        }

        private void AddFaceIndices(in NativeList<ushort> indices, int aof1, int aof2, int aof3, int aof4, bool force = false) {
            int vertexCount = Vertices.Length;
            if (force || aof1 + aof3 < aof2 + aof4) {
                // Fliped quad.
                indices.Add((ushort)(0 + vertexCount));
                indices.Add((ushort)(1 + vertexCount));
                indices.Add((ushort)(3 + vertexCount));
                indices.Add((ushort)(3 + vertexCount));
                indices.Add((ushort)(1 + vertexCount));
                indices.Add((ushort)(2 + vertexCount));
            } else {
                // Normal quad.
                indices.Add((ushort)(0 + vertexCount));
                indices.Add((ushort)(1 + vertexCount));
                indices.Add((ushort)(2 + vertexCount));
                indices.Add((ushort)(0 + vertexCount));
                indices.Add((ushort)(2 + vertexCount));
                indices.Add((ushort)(3 + vertexCount));
            }
        }

        private void GetVoxel(in int3 localVoxelCoordinate, out Voxel voxel) {
            var voxelCoordinate = ChunkCoordinate * Chunk.Size + localVoxelCoordinate;
            var sideChunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);

            var sideLocalVoxelCoordinate = voxelCoordinate - sideChunkCoordinate * Chunk.Size;

            sideChunkCoordinate -= ChunkCoordinate;
            sideChunkCoordinate += new int3(1, 1, 1);
            var clasterIndex = IndexUtility.ToIndex(sideChunkCoordinate, 3, 3);
            var voxels = Claster[clasterIndex];
            if (!voxels.IsCreated) {
                voxel = default;
                return;
            }

            var sideLocalVoxelIndex = IndexUtility.ToIndex(sideLocalVoxelCoordinate, Chunk.Size, Chunk.Size);
            voxel = voxels[sideLocalVoxelIndex];
        }

        private byte GetLight(int x, int y, int z, LightChanel chanel) {
            GetVoxel(new int3(x, y, z), out var voxel);
            return voxel.Light.Get(chanel);
        }

        private bool IsTransparent(int x, int y, int z) {
            GetVoxel(new int3(x, y, z), out var voxel);
            return Blocks[(int)voxel.Type].IsTransparent;
        }

        private bool HasFace(in int3 localVoxelCoordinate, BlockType blockType) {
            GetVoxel(localVoxelCoordinate, out var voxel);
            return Blocks[(int)voxel.Type].IsTransparent && voxel.Type != blockType;
        }

        public void Dispose() {
            Claster.Dispose();
        }
    }
}