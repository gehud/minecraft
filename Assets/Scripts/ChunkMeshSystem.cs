using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft {
    [BurstCompile]
    [UpdateAfter(typeof(ChunkMeshDataSystem))]
    public partial struct ChunkMeshSystem : ISystem {
        public const MeshUpdateFlags UpdateFlags
            = MeshUpdateFlags.DontRecalculateBounds
            | MeshUpdateFlags.DontResetBoneBounds
            | MeshUpdateFlags.DontNotifyMeshUsers
#if !DEBUG
            | MeshUpdateFlags.DontValidateIndices;
#else
            ;
#endif

        private const float chunkSizeHalf = Chunk.Size / 2.0f;

        private NativeArray<VertexAttributeDescriptor> descriptors;

        private struct ScheduledJob {
            public ChunkMeshJob Data;
            public JobHandle Handle;
        }

        private NativeList<ScheduledJob> jobs;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state) {
            descriptors = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Persistent);
            descriptors[0] = new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.UInt32, 2);
            
            jobs = new NativeList<ScheduledJob>(Allocator.Persistent);
        }

        private void ApplyJob(ref SystemState state, in ChunkMeshJob job) {
            var entity = job.Entity;

            var mesh = new Mesh();
            Mesh.ApplyAndDisposeWritableMeshData(job.MeshDataArray, mesh, UpdateFlags);

            if (!state.EntityManager.HasComponent<RenderMeshArray>(entity)) {
                var materials = new Material[] {
                    state.EntityManager.GetComponentObject<ChunkMeshSystemData>(state.SystemHandle).OpaqueMaterial,
                    state.EntityManager.GetComponentObject<ChunkMeshSystemData>(state.SystemHandle).TransparentMaterial
                };

                var meshes = new Mesh[] {
                    mesh,
                };

                RenderMeshUtility.AddComponents(
                    entity,
                    state.EntityManager,
                    new RenderMeshDescription(ShadowCastingMode.Off),
                    new RenderMeshArray(materials, meshes),
                    MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0, 0)
                );

                state.EntityManager.SetComponentData(entity, new RenderBounds {
                    Value = new AABB {
                        Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf),
                        Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                    }
                });

                var coordinate = state.EntityManager.GetComponentData<Chunk>(entity).Coordinate;
                var position = coordinate * Chunk.Size;

                state.EntityManager.SetComponentData(entity, new WorldRenderBounds {
                    Value = new AABB {
                        Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf) + position,
                        Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                    }
                });

                var rendererEntity = state.EntityManager.CreateEntity();
                state.EntityManager.SetName(rendererEntity, $"TransparentChunk({coordinate.x}, {coordinate.y}, {coordinate.z})");

                RenderMeshUtility.AddComponents(
                    rendererEntity,
                    state.EntityManager,
                    new RenderMeshDescription(ShadowCastingMode.Off),
                    state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity),
                    MaterialMeshInfo.FromRenderMeshArrayIndices(1, 0, 1)
                );

                state.EntityManager.SetComponentData(rendererEntity, new RenderBounds {
                    Value = new AABB {
                        Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf),
                        Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                    }
                });

                state.EntityManager.SetComponentData(rendererEntity, new WorldRenderBounds {
                    Value = new AABB {
                        Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf) + position,
                        Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                    }
                });

                state.EntityManager.AddComponentData(rendererEntity, new LocalToWorld {
                    Value = float4x4.Translate(position)
                });

                var buffer = state.EntityManager.AddBuffer<SubChunk>(entity);
                buffer.Add(new SubChunk {
                    Value = rendererEntity
                });
            } else {
                state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity).Meshes[0] = mesh;
            }

            state.EntityManager.RemoveComponent<ChunkMeshData>(entity);
        }

        private bool TryCompleteJob(ref SystemState state, in ScheduledJob job) {
            if (!job.Handle.IsCompleted) {
                return false;
            }

            job.Handle.Complete();

            ApplyJob(ref state, job.Data);
            state.EntityManager.SetComponentEnabled<ThreadedChunk>(job.Data.Entity, false);

            return true;
        }

        void ISystem.OnUpdate(ref SystemState state) {
            var querry = SystemAPI.QueryBuilder()
                .WithAll<ChunkMeshData>()
                .WithNone<ThreadedChunk>()
                .Build();

            var entities = querry.ToEntityArray(Allocator.Temp);

            foreach (var entity in entities) {
                var data = state.EntityManager.GetComponentData<ChunkMeshData>(entity);

                var job = new ChunkMeshJob {
                    Entity = entity,
                    MeshData = data,
                    MeshDataArray = Mesh.AllocateWritableMeshData(1),
                    Descriptors = descriptors
                };

                if (state.EntityManager.IsComponentEnabled<ImmediateChunk>(entity)) {
                    job.Run();
                    ApplyJob(ref state, job);
                    state.EntityManager.SetComponentEnabled<ImmediateChunk>(entity, false);
                } else {
                    var handle = job.Schedule();
                    state.EntityManager.SetComponentEnabled<ThreadedChunk>(entity, true);
                    jobs.Add(new ScheduledJob {
                        Data = job,
                        Handle = handle
                    });
                }
            }

            for (int i = 0; i < jobs.Length; i++) {
                var job = jobs[i];

                if (TryCompleteJob(ref state, job)) {
                    jobs.RemoveAt(i);
                }
            }

            entities.Dispose();
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state) {
            foreach (var job in jobs) {
                job.Handle.Complete();
            }

            descriptors.Dispose();
            jobs.Dispose();
        }
    }
}