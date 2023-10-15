using Minecraft.Components;
using Minecraft.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

namespace Minecraft.Systems {
    [UpdateAfter(typeof(ChunkGenerationSystem))]
    public partial class ChunkMeshDataSystem : SystemBase {
        private MeshJob lastJob;
        private JobHandle lastJobHandle;

        private SystemHandle chunkBufferingSystem;

        protected override void OnCreate() {
            chunkBufferingSystem = World.GetExistingSystem<ChunkBufferingSystem>();
        }

        [BurstCompile]
        private struct MeshJob : IJob {
            public NativeList<Vertex> Vertices;
            public NativeList<ushort> Indices;
            [ReadOnly] 
            public Entity Entity;
            [ReadOnly] 
            public int3 ChunkCoordinate;
            [ReadOnly] 
            public NativeArray<Voxel> Claster;
            [ReadOnly] 
            public NativeArray<BlockDescription> Blocks;

            public void Execute() {
                for (int x = 0; x < Chunk.SIZE; x++) {
                    for (int y = 0; y < Chunk.SIZE; y++) {
                        for (int z = 0; z < Chunk.SIZE; z++) {
                            var localVoxelCoordinate = new int3(x, y, z);
                            var voxel = GetVoxel(Claster, ChunkCoordinate, localVoxelCoordinate);

                            if (voxel.Type == BlockType.Air) {
                                continue;
                            }

                            var texturing = Blocks[(int)voxel.Type].Texturing;
                            float uvStep = 16.0f / 256.0f;

                            // Right face
                            if (HasFace(Claster, ChunkCoordinate, localVoxelCoordinate + new int3(1, 0, 0))) {
                                var vertexCount = Vertices.Length;
                                Indices.Add((ushort)(vertexCount + 0));
                                Indices.Add((ushort)(vertexCount + 1));
                                Indices.Add((ushort)(vertexCount + 2));
                                Indices.Add((ushort)(vertexCount + 0));
                                Indices.Add((ushort)(vertexCount + 2));
                                Indices.Add((ushort)(vertexCount + 3));

                                Vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 0.0f, texturing.Right.x * uvStep + uvStep * 0.0f, texturing.Right.y * uvStep + uvStep * 0.0f));
                                Vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 0.0f, texturing.Right.x * uvStep + uvStep * 0.0f, texturing.Right.y * uvStep + uvStep * 1.0f));
                                Vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 1.0f, texturing.Right.x * uvStep + uvStep * 1.0f, texturing.Right.y * uvStep + uvStep * 1.0f));
                                Vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 1.0f, texturing.Right.x * uvStep + uvStep * 1.0f, texturing.Right.y * uvStep + uvStep * 0.0f));
                            }

                            // Left face
                            if (HasFace(Claster, ChunkCoordinate, localVoxelCoordinate + new int3(-1, 0, 0))) {
                                var vertexCount = Vertices.Length;
                                Indices.Add((ushort)(vertexCount + 0));
                                Indices.Add((ushort)(vertexCount + 1));
                                Indices.Add((ushort)(vertexCount + 2));
                                Indices.Add((ushort)(vertexCount + 0));
                                Indices.Add((ushort)(vertexCount + 2));
                                Indices.Add((ushort)(vertexCount + 3));

                                Vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 1.0f, texturing.Left.x * uvStep + uvStep * 0.0f, texturing.Left.y * uvStep + uvStep * 0.0f));
                                Vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 1.0f, texturing.Left.x * uvStep + uvStep * 0.0f, texturing.Left.y * uvStep + uvStep * 1.0f));
                                Vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 0.0f, texturing.Left.x * uvStep + uvStep * 1.0f, texturing.Left.y * uvStep + uvStep * 1.0f));
                                Vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 0.0f, texturing.Left.x * uvStep + uvStep * 1.0f, texturing.Left.y * uvStep + uvStep * 0.0f));
                            }

                            // Top face
                            if (HasFace(Claster, ChunkCoordinate, localVoxelCoordinate + new int3(0, 1, 0))) {
                                var vertexCount = Vertices.Length;
                                Indices.Add((ushort)(vertexCount + 0));
                                Indices.Add((ushort)(vertexCount + 1));
                                Indices.Add((ushort)(vertexCount + 2));
                                Indices.Add((ushort)(vertexCount + 0));
                                Indices.Add((ushort)(vertexCount + 2));
                                Indices.Add((ushort)(vertexCount + 3));

                                Vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 0.0f, texturing.Top.x * uvStep + uvStep * 0.0f, texturing.Top.y * uvStep + uvStep * 0.0f));
                                Vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 1.0f, texturing.Top.x * uvStep + uvStep * 0.0f, texturing.Top.y * uvStep + uvStep * 1.0f));
                                Vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 1.0f, texturing.Top.x * uvStep + uvStep * 1.0f, texturing.Top.y * uvStep + uvStep * 1.0f));
                                Vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 0.0f, texturing.Top.x * uvStep + uvStep * 1.0f, texturing.Top.y * uvStep + uvStep * 0.0f));
                            }

                            // Buttom face
                            if (HasFace(Claster, ChunkCoordinate, localVoxelCoordinate + new int3(0, -1, 0))) {
                                var vertexCount = Vertices.Length;
                                Indices.Add((ushort)(vertexCount + 0));
                                Indices.Add((ushort)(vertexCount + 1));
                                Indices.Add((ushort)(vertexCount + 2));
                                Indices.Add((ushort)(vertexCount + 0));
                                Indices.Add((ushort)(vertexCount + 2));
                                Indices.Add((ushort)(vertexCount + 3));

                                Vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 0.0f, texturing.Bottom.x * uvStep + uvStep * 0.0f, texturing.Bottom.y * uvStep + uvStep * 0.0f));
                                Vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 1.0f, texturing.Bottom.x * uvStep + uvStep * 0.0f, texturing.Bottom.y * uvStep + uvStep * 1.0f));
                                Vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 1.0f, texturing.Bottom.x * uvStep + uvStep * 1.0f, texturing.Bottom.y * uvStep + uvStep * 1.0f));
                                Vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 0.0f, texturing.Bottom.x * uvStep + uvStep * 1.0f, texturing.Bottom.y * uvStep + uvStep * 0.0f));
                            }

                            // Front face
                            if (HasFace(Claster, ChunkCoordinate, localVoxelCoordinate + new int3(0, 0, 1))) {
                                var vertexCount = Vertices.Length;
                                Indices.Add((ushort)(vertexCount + 0));
                                Indices.Add((ushort)(vertexCount + 1));
                                Indices.Add((ushort)(vertexCount + 2));
                                Indices.Add((ushort)(vertexCount + 0));
                                Indices.Add((ushort)(vertexCount + 2));
                                Indices.Add((ushort)(vertexCount + 3));

                                Vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 1.0f, texturing.Front.x * uvStep + uvStep * 0.0f, texturing.Front.y * uvStep + uvStep * 0.0f));
                                Vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 1.0f, texturing.Front.x * uvStep + uvStep * 0.0f, texturing.Front.y * uvStep + uvStep * 1.0f));
                                Vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 1.0f, texturing.Front.x * uvStep + uvStep * 1.0f, texturing.Front.y * uvStep + uvStep * 1.0f));
                                Vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 1.0f, texturing.Front.x * uvStep + uvStep * 1.0f, texturing.Front.y * uvStep + uvStep * 0.0f));
                            }

                            // Back face
                            if (HasFace(Claster, ChunkCoordinate, localVoxelCoordinate + new int3(0, 0, -1))) {
                                var vertexCount = Vertices.Length;
                                Indices.Add((ushort)(vertexCount + 0));
                                Indices.Add((ushort)(vertexCount + 1));
                                Indices.Add((ushort)(vertexCount + 2));
                                Indices.Add((ushort)(vertexCount + 0));
                                Indices.Add((ushort)(vertexCount + 2));
                                Indices.Add((ushort)(vertexCount + 3));

                                Vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 0.0f, texturing.Back.x * uvStep + uvStep * 0.0f, texturing.Back.y * uvStep + uvStep * 0.0f));
                                Vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 0.0f, texturing.Back.x * uvStep + uvStep * 0.0f, texturing.Back.y * uvStep + uvStep * 1.0f));
                                Vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 0.0f, texturing.Back.x * uvStep + uvStep * 1.0f, texturing.Back.y * uvStep + uvStep * 1.0f));
                                Vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 0.0f, texturing.Back.x * uvStep + uvStep * 1.0f, texturing.Back.y * uvStep + uvStep * 0.0f));
                            }
                        }
                    }
                }
            }
        }

        private void ScheduleSinsgleJob(NativeArray<Entity> entities) {
            if (!lastJobHandle.IsCompleted) {
                return;
            }

            lastJobHandle.Complete();

            var lastEntity = lastJob.Entity;

            if (lastJob.Claster.IsCreated) {
                lastJob.Claster.Dispose();
            }

            if (EntityManager.Exists(lastEntity)) {
                EntityManager.AddComponentData(lastEntity, new ChunkMeshData {
                    Vertices = lastJob.Vertices.AsArray(),
                    Indices = lastJob.Indices.AsArray()
                });

                EntityManager.RemoveComponent<DirtyChunk>(lastEntity);
            } else {
                lastJob.Vertices.Dispose();
                lastJob.Indices.Dispose();
            }

            lastJob = default;

            for (int i = 0; i < entities.Length; i++) {
                var entity = entities[i];

                if (entity == lastEntity) {
                    continue;
                }

                var claster = new NativeArray<Voxel>(3 * 3 * 3 * Chunk.VOLUME, Allocator.TempJob);
                var chunkCoordinate = EntityManager.GetComponentData<Chunk>(entity).Coordinate;
                var origin = chunkCoordinate - new int3(1, 1, 1);
                var chunkBuffer = EntityManager.GetComponentDataRW<ChunkBuffer>(chunkBufferingSystem);
                bool isValidClaster = true;
                for (int j = 0; j < 3 * 3 * 3; j++) {
                    var coordinate = origin + Array3DUtility.To3D(j, 3, 3);
                    var chunk = ChunkBufferingSystem.GetChunk(chunkBuffer.ValueRO, coordinate);
                    bool isValidChunk = EntityManager.Exists(chunk) && EntityManager.HasComponent<Chunk>(chunk) && !EntityManager.HasComponent<RawChunk>(chunk);

                    if (isValidChunk) {
                        var sourceSlice = new NativeSlice<Voxel>(EntityManager.GetComponentData<Chunk>(chunk).Voxels);
                        var destinationSlice = new NativeSlice<Voxel>(claster, j * Chunk.VOLUME, Chunk.VOLUME);
                        destinationSlice.CopyFrom(sourceSlice);
                    } else if (coordinate.y != -1 && coordinate.y != ChunkBuffer.HEIGHT && !isValidChunk) {
                        isValidClaster = false;
                        break;
                    }
                }

                if (!isValidClaster) {
                    claster.Dispose();
                    continue;
                }

                lastJob = new MeshJob {
                    Vertices = new NativeList<Vertex>(Allocator.TempJob),
                    Indices = new NativeList<ushort>(Allocator.TempJob),
                    ChunkCoordinate = chunkCoordinate,
                    Entity = entity,
                    Claster = claster,
                    Blocks = StaticBlockDatabase.Data
                };

                lastJobHandle = lastJob.Schedule();

                return;
            }
        }

        protected override void OnUpdate() {
            var querry = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Chunk>()
                .WithAll<DirtyChunk>()
                .WithNone<DisableRendering>()
                .Build(EntityManager);

            var entities = querry.ToEntityArray(Allocator.Temp);
            querry.Dispose();

            ScheduleSinsgleJob(entities);

            entities.Dispose();
        }

        private static Voxel GetVoxel(in NativeArray<Voxel> claster, in int3 chunkCoordinate, int3 localVoxelCoordinate) {
            var voxelCoordinate = chunkCoordinate * Chunk.SIZE + localVoxelCoordinate;
            var sideChunkCoordinate = new int3 {
                x = (int)math.floor(voxelCoordinate.x / (float)Chunk.SIZE),
                y = (int)math.floor(voxelCoordinate.y / (float)Chunk.SIZE),
                z = (int)math.floor(voxelCoordinate.z / (float)Chunk.SIZE)
            };

            var sideLocalVoxelCoordinate = voxelCoordinate - sideChunkCoordinate * Chunk.SIZE;

            sideChunkCoordinate -= chunkCoordinate;
            sideChunkCoordinate += new int3(1, 1, 1);
            var clasterIndex = Array3DUtility.To1D(sideChunkCoordinate, 3, 3);
            var sideLocalVoxelIndex = Array3DUtility.To1D(sideLocalVoxelCoordinate, Chunk.SIZE, Chunk.SIZE);
            return claster[clasterIndex * Chunk.VOLUME + sideLocalVoxelIndex];
        }

        private static bool HasFace(in NativeArray<Voxel> claster, in int3 chunkCoordinate, int3 localVoxelCoordinate) {
            return GetVoxel(claster, chunkCoordinate, localVoxelCoordinate).Type == BlockType.Air;
        }
    }
}