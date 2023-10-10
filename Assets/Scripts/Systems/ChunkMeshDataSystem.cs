using Minecraft.Components;
using Minecraft.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Minecraft.Systems
{
    [UpdateAfter(typeof(ChunkGenerationSystem))]
    public partial class ChunkMeshDataSystem : SystemBase {
		private ChunkMeshDataGenerationJob lastJob;
		private JobHandle lastJobHandle;
		private EntityCommandBuffer cmd;

        private Vector3 processed = Vector3.one * float.PositiveInfinity;

        protected override void OnCreate() {
            EntityManager.AddComponentData(SystemHandle, new ChunkMeshDataSystemData {
                ChunkBufferingSystem = World.GetExistingSystem<ChunkBufferingSystem>()
            });
        }

        [BurstCompile]
        private struct ChunkMeshDataGenerationJob : IJob {
            public EntityCommandBuffer CommandBuffer;
            [ReadOnly] public Entity Entity;
            [ReadOnly] public int3 ChunkCoordinate;
            [ReadOnly, NativeDisableContainerSafetyRestriction]
            public NativeArray<NativeArray<Voxel>> Claster;
            [ReadOnly] public NativeArray<BlockDescription> Blocks; 

            public void Execute() {
                var vertices = new NativeList<Vertex>(Allocator.Persistent);
                var indices = new NativeList<ushort>(Allocator.Persistent);

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
                                var vertexCount = vertices.Length;
                                indices.Add((ushort)(vertexCount + 0));
                                indices.Add((ushort)(vertexCount + 1));
                                indices.Add((ushort)(vertexCount + 2));
                                indices.Add((ushort)(vertexCount + 0));
                                indices.Add((ushort)(vertexCount + 2));
                                indices.Add((ushort)(vertexCount + 3));

                                vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 0.0f, texturing.Right.x * uvStep + uvStep * 0.0f, texturing.Right.y * uvStep + uvStep * 0.0f));
                                vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 0.0f, texturing.Right.x * uvStep + uvStep * 0.0f, texturing.Right.y * uvStep + uvStep * 1.0f));
                                vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 1.0f, texturing.Right.x * uvStep + uvStep * 1.0f, texturing.Right.y * uvStep + uvStep * 1.0f));
                                vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 1.0f, texturing.Right.x * uvStep + uvStep * 1.0f, texturing.Right.y * uvStep + uvStep * 0.0f));
                            }

                            // Left face
                            if (HasFace(Claster, ChunkCoordinate, localVoxelCoordinate + new int3(-1, 0, 0))) {
                                var vertexCount = vertices.Length;
                                indices.Add((ushort)(vertexCount + 0));
                                indices.Add((ushort)(vertexCount + 1));
                                indices.Add((ushort)(vertexCount + 2));
                                indices.Add((ushort)(vertexCount + 0));
                                indices.Add((ushort)(vertexCount + 2));
                                indices.Add((ushort)(vertexCount + 3));

                                vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 1.0f, texturing.Left.x * uvStep + uvStep * 0.0f, texturing.Left.y * uvStep + uvStep * 0.0f));
                                vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 1.0f, texturing.Left.x * uvStep + uvStep * 0.0f, texturing.Left.y * uvStep + uvStep * 1.0f));
                                vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 0.0f, texturing.Left.x * uvStep + uvStep * 1.0f, texturing.Left.y * uvStep + uvStep * 1.0f));
                                vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 0.0f, texturing.Left.x * uvStep + uvStep * 1.0f, texturing.Left.y * uvStep + uvStep * 0.0f));
                            }

                            // Top face
                            if (HasFace(Claster, ChunkCoordinate, localVoxelCoordinate + new int3(0, 1, 0))) {
                                var vertexCount = vertices.Length;
                                indices.Add((ushort)(vertexCount + 0));
                                indices.Add((ushort)(vertexCount + 1));
                                indices.Add((ushort)(vertexCount + 2));
                                indices.Add((ushort)(vertexCount + 0));
                                indices.Add((ushort)(vertexCount + 2));
                                indices.Add((ushort)(vertexCount + 3));

                                vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 0.0f, texturing.Top.x * uvStep + uvStep * 0.0f, texturing.Top.y * uvStep + uvStep * 0.0f));
                                vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 1.0f, texturing.Top.x * uvStep + uvStep * 0.0f, texturing.Top.y * uvStep + uvStep * 1.0f));
                                vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 1.0f, texturing.Top.x * uvStep + uvStep * 1.0f, texturing.Top.y * uvStep + uvStep * 1.0f));
                                vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 0.0f, texturing.Top.x * uvStep + uvStep * 1.0f, texturing.Top.y * uvStep + uvStep * 0.0f));
                            }

                            // Buttom face
                            if (HasFace(Claster, ChunkCoordinate, localVoxelCoordinate + new int3(0, -1, 0))) {
                                var vertexCount = vertices.Length;
                                indices.Add((ushort)(vertexCount + 0));
                                indices.Add((ushort)(vertexCount + 1));
                                indices.Add((ushort)(vertexCount + 2));
                                indices.Add((ushort)(vertexCount + 0));
                                indices.Add((ushort)(vertexCount + 2));
                                indices.Add((ushort)(vertexCount + 3));

                                vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 0.0f, texturing.Bottom.x * uvStep + uvStep * 0.0f, texturing.Bottom.y * uvStep + uvStep * 0.0f));
                                vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 1.0f, texturing.Bottom.x * uvStep + uvStep * 0.0f, texturing.Bottom.y * uvStep + uvStep * 1.0f));
                                vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 1.0f, texturing.Bottom.x * uvStep + uvStep * 1.0f, texturing.Bottom.y * uvStep + uvStep * 1.0f));
                                vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 0.0f, texturing.Bottom.x * uvStep + uvStep * 1.0f, texturing.Bottom.y * uvStep + uvStep * 0.0f));
                            }

                            // Front face
                            if (HasFace(Claster, ChunkCoordinate, localVoxelCoordinate + new int3(0, 0, 1))) {
                                var vertexCount = vertices.Length;
                                indices.Add((ushort)(vertexCount + 0));
                                indices.Add((ushort)(vertexCount + 1));
                                indices.Add((ushort)(vertexCount + 2));
                                indices.Add((ushort)(vertexCount + 0));
                                indices.Add((ushort)(vertexCount + 2));
                                indices.Add((ushort)(vertexCount + 3));

                                vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 1.0f, texturing.Front.x * uvStep + uvStep * 0.0f, texturing.Front.y * uvStep + uvStep * 0.0f));
                                vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 1.0f, texturing.Front.x * uvStep + uvStep * 0.0f, texturing.Front.y * uvStep + uvStep * 1.0f));
                                vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 1.0f, texturing.Front.x * uvStep + uvStep * 1.0f, texturing.Front.y * uvStep + uvStep * 1.0f));
                                vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 1.0f, texturing.Front.x * uvStep + uvStep * 1.0f, texturing.Front.y * uvStep + uvStep * 0.0f));
                            }

                            // Back face
                            if (HasFace(Claster, ChunkCoordinate, localVoxelCoordinate + new int3(0, 0, -1))) {
                                var vertexCount = vertices.Length;
                                indices.Add((ushort)(vertexCount + 0));
                                indices.Add((ushort)(vertexCount + 1));
                                indices.Add((ushort)(vertexCount + 2));
                                indices.Add((ushort)(vertexCount + 0));
                                indices.Add((ushort)(vertexCount + 2));
                                indices.Add((ushort)(vertexCount + 3));

                                vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 0.0f, texturing.Back.x * uvStep + uvStep * 0.0f, texturing.Back.y * uvStep + uvStep * 0.0f));
                                vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 0.0f, texturing.Back.x * uvStep + uvStep * 0.0f, texturing.Back.y * uvStep + uvStep * 1.0f));
                                vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 0.0f, texturing.Back.x * uvStep + uvStep * 1.0f, texturing.Back.y * uvStep + uvStep * 1.0f));
                                vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 0.0f, texturing.Back.x * uvStep + uvStep * 1.0f, texturing.Back.y * uvStep + uvStep * 0.0f));
                            }
                        }
                    }
                }

                CommandBuffer.AddComponent(Entity, new ChunkMeshData {
                    Vertices = vertices.AsArray(),
                    Indices = indices.AsArray()
                });

                CommandBuffer.RemoveComponent<DirtyChunk>(Entity);
            }
        }

        protected override void OnUpdate() {
            var querry = new EntityQueryBuilder(Allocator.TempJob).
                WithAll<Chunk>().
                WithAll<DirtyChunk>().
                WithNone<DisableRendering>().
                Build(EntityManager);
            var entities = querry.ToEntityArray(Allocator.TempJob);
            querry.Dispose();
			if (entities.Length != 0 && lastJobHandle.IsCompleted) {
				lastJobHandle.Complete();

				if (cmd.IsCreated) {
					cmd.Playback(EntityManager);
					cmd.Dispose();
				}

				if (lastJob.Claster.IsCreated) {
					lastJob.Claster.Dispose();
				}

				cmd = new EntityCommandBuffer(Allocator.Persistent);

				var entity = entities[0];

                var claster = new NativeArray<NativeArray<Voxel>>(3 * 3 * 3, Allocator.TempJob);
                var chunkCoordinate = EntityManager.GetComponentData<Chunk>(entity).Coordinate;
                var origin = chunkCoordinate - new int3(1, 1, 1);
                var chunkBuffeingSystem = EntityManager.GetComponentData<ChunkMeshDataSystemData>(SystemHandle).ChunkBufferingSystem;
                var chunkBuffer = EntityManager.GetComponentDataRW<ChunkBuffer>(chunkBuffeingSystem);
                for (int i = 0; i < 3 * 3 * 3; i++) {
                    var coordinate = Array3DUtility.To3D(i, 3, 3);
                    var chunk = ChunkBufferingSystem.GetChunk(chunkBuffer.ValueRO, origin + coordinate);
                    claster[i] = EntityManager.Exists(chunk) && EntityManager.HasComponent<Chunk>(entity) ? EntityManager.GetComponentData<Chunk>(chunk).Voxels : default;
                }

				lastJob = new ChunkMeshDataGenerationJob {
                    ChunkCoordinate = chunkCoordinate,
                    CommandBuffer = cmd,
                    Entity = entity,
                    Claster = claster,
                    Blocks = StaticBlockDatabase.Data
                };

                processed = new Vector3
                {
                    x = lastJob.ChunkCoordinate.x,
                    y = lastJob.ChunkCoordinate.y,
                    z = lastJob.ChunkCoordinate.z
                } * Chunk.SIZE;

				lastJobHandle = lastJob.Schedule();
            } else {
                processed = Vector3.one * float.PositiveInfinity;
            }

            Debug.DrawLine(processed, processed + Vector3.right * Chunk.SIZE, Color.green);
            Debug.DrawLine(processed, processed + Vector3.up * Chunk.SIZE, Color.green);
            Debug.DrawLine(processed, processed + Vector3.forward * Chunk.SIZE, Color.green);

            entities.Dispose();
        }

        private static Voxel GetVoxel(in NativeArray<NativeArray<Voxel>> claster, in int3 chunkCoordinate, int3 localVoxelCoordinate) {
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
            if (!claster[clasterIndex].IsCreated) {
                return new Voxel(BlockType.Air);
            }

            var sideLocalVoxelIndex = Array3DUtility.To1D(sideLocalVoxelCoordinate, Chunk.SIZE, Chunk.SIZE);
            return claster[clasterIndex][sideLocalVoxelIndex];
        }

        private static bool HasFace(in NativeArray<NativeArray<Voxel>> claster, in int3 chunkCoordinate, int3 localVoxelCoordinate) {
            return GetVoxel(claster, chunkCoordinate, localVoxelCoordinate).Type == 0;
        }
    }
}