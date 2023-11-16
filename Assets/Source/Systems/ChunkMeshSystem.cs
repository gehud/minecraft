using Minecraft.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft.Systems {
    [UpdateAfter(typeof(ChunkMeshDataSystem))]
    public partial class ChunkMeshSystem : SystemBase {
        private const MeshUpdateFlags UpdateFlags
            = MeshUpdateFlags.DontRecalculateBounds
            | MeshUpdateFlags.DontResetBoneBounds
            | MeshUpdateFlags.DontNotifyMeshUsers
#if !DEBUG
			| MeshUpdateFlags.DontValidateIndices;
#else
            ;
#endif

        private const float chunkSizeHalf = Chunk.Size / 2.0f;

        private MeshJob lastJob;
        private JobHandle lastJobHandle;

        [BurstCompile]
        private struct MeshJob : IJob {
            public Entity Entity;
            public ChunkMeshData MeshData;
            public Mesh.MeshDataArray MeshDataArray;

            public void Execute() {
                var mesh = MeshDataArray[0];

                var descriptors = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Temp);
                descriptors[0] = new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.UInt32, 2);
                mesh.SetVertexBufferParams(MeshData.Vertices.Length, descriptors);
                descriptors.Dispose();

                var vertices = mesh.GetVertexData<Vertex>();
                for (int i = 0; i < MeshData.Vertices.Length; i++) {
                    vertices[i] = MeshData.Vertices[i];
                }

                var opaqueIndicesCount = MeshData.OpaqueIndices.Length;
                var transparentIndicesCount = MeshData.TransparentIndices.Length;
                var indicesCount = opaqueIndicesCount + transparentIndicesCount;
                mesh.SetIndexBufferParams(indicesCount, IndexFormat.UInt16);
                var indices = mesh.GetIndexData<ushort>();

                for (int i = 0; i < opaqueIndicesCount; i++) {
                    indices[i] = MeshData.OpaqueIndices[i];
                }

                for (int i = 0; i < transparentIndicesCount; i++) {
                    indices[i + opaqueIndicesCount] = MeshData.TransparentIndices[i];
                }

                mesh.subMeshCount = 2;
                mesh.SetSubMesh(0, new SubMeshDescriptor(0, opaqueIndicesCount), UpdateFlags);
                mesh.SetSubMesh(1, new SubMeshDescriptor(opaqueIndicesCount, transparentIndicesCount), UpdateFlags);
            }
        }

        private void ScheduleSingleJob(NativeArray<Entity> entities) {
            if (!lastJobHandle.IsCompleted) {
                return;
            }

            lastJobHandle.Complete();

            var lastEntity = lastJob.Entity;

            if (EntityManager.Exists(lastEntity)) {
                var mesh = new Mesh();
                Mesh.ApplyAndDisposeWritableMeshData(lastJob.MeshDataArray, mesh, UpdateFlags);

                if (!EntityManager.HasComponent<RenderMeshArray>(lastEntity)) {
                    var materials = new Material[] {
                        EntityManager.GetComponentObject<ChunkMeshSystemData>(SystemHandle).OpaqueMaterial,
                        EntityManager.GetComponentObject<ChunkMeshSystemData>(SystemHandle).TransparentMaterial
                    };

                    var meshes = new Mesh[] {
                        mesh,
                    };

                    RenderMeshUtility.AddComponents(
                        lastEntity,
                        EntityManager,
                        new RenderMeshDescription(ShadowCastingMode.Off),
                        new RenderMeshArray(materials, meshes),
                        MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0, 0)
                    );

                    EntityManager.SetComponentData(lastEntity, new RenderBounds {
                        Value = new AABB {
                            Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf),
                            Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                        }
                    });

                    var position = EntityManager.GetComponentData<Chunk>(lastEntity).Coordinate * Chunk.Size;

                    EntityManager.SetComponentData(lastEntity, new WorldRenderBounds {
                        Value = new AABB {
                            Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf) + position,
                            Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                        }
                    });

                    var rendererEntity = EntityManager.CreateEntity();

                    RenderMeshUtility.AddComponents(
                        rendererEntity,
                        EntityManager,
                        new RenderMeshDescription(ShadowCastingMode.Off),
                        EntityManager.GetSharedComponentManaged<RenderMeshArray>(lastEntity),
                        MaterialMeshInfo.FromRenderMeshArrayIndices(1, 0, 1)
                    );

                    EntityManager.SetComponentData(rendererEntity, new RenderBounds {
                        Value = new AABB {
                            Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf),
                            Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                        }
                    });

                    EntityManager.SetComponentData(rendererEntity, new WorldRenderBounds {
                        Value = new AABB {
                            Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf) + position,
                            Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                        }
                    });

                    EntityManager.AddComponentData(rendererEntity, new LocalToWorld {
                        Value = float4x4.Translate(position)
                    });

                    var buffer = EntityManager.AddBuffer<LinkedEntityGroup>(lastEntity);
                    var linkedEntityGroup = new LinkedEntityGroup {
                        Value = rendererEntity
                    };
                    buffer.Add(linkedEntityGroup);
                } else {
                    EntityManager.GetSharedComponentManaged<RenderMeshArray>(lastEntity).Meshes[0] = mesh;
                }

                EntityManager.RemoveComponent<ChunkMeshData>(lastEntity);
            } else if (lastJob.MeshDataArray.Length != 0) {
                lastJob.MeshDataArray.Dispose();
            }

            lastJob = default;

            for (int i = 0; i < entities.Length; i++) {
                var entity = entities[i];

                if (entity == lastEntity) {
                    continue;
                }

                lastJob = new MeshJob {
                    Entity = entity,
                    MeshData = EntityManager.GetComponentData<ChunkMeshData>(entity),
                    MeshDataArray = Mesh.AllocateWritableMeshData(1)
                };

                lastJobHandle = lastJob.Schedule();

                return;
            }
        }

        protected override void OnUpdate() {
            Entities.WithAll<ImmediateChunk>().ForEach((Entity entity, in ChunkMeshData chunkMeshData, in RenderMeshArray renderMeshArray) => {
                var job = new MeshJob {
                    MeshData = chunkMeshData,
                    MeshDataArray = Mesh.AllocateWritableMeshData(1)
                };

                job.Schedule().Complete();

                var mesh = new Mesh();
                Mesh.ApplyAndDisposeWritableMeshData(job.MeshDataArray, mesh, UpdateFlags);

                EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity).Meshes[0] = mesh;
                EntityManager.RemoveComponent<ChunkMeshData>(entity);
                EntityManager.RemoveComponent<ImmediateChunk>(entity);
            }).WithStructuralChanges().Run();

            var querry = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ChunkMeshData>()
                .WithNone<ImmediateChunk>()
                .Build(EntityManager);

            var entities = querry.ToEntityArray(Allocator.TempJob);
            querry.Dispose();

            ScheduleSingleJob(entities);

            entities.Dispose();
        }
    }
}