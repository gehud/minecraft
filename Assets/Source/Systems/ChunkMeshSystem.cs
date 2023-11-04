using Minecraft.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft.Systems {
    [UpdateAfter(typeof(ChunkMeshDataSystem))]
    public partial class ChunkMeshSystem : SystemBase {
        private const MeshUpdateFlags MESH_UPDATE_FLAGS
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

                mesh.SetIndexBufferParams(MeshData.Indices.Length, IndexFormat.UInt16);
                var indices = mesh.GetIndexData<ushort>();
                for (int i = 0; i < MeshData.Indices.Length; i++) {
                    indices[i] = MeshData.Indices[i];
                }

                mesh.subMeshCount = 1;
                mesh.SetSubMesh(0, new SubMeshDescriptor(0, MeshData.Indices.Length), MESH_UPDATE_FLAGS);
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
                Mesh.ApplyAndDisposeWritableMeshData(lastJob.MeshDataArray, mesh, MESH_UPDATE_FLAGS);
                
                if (!EntityManager.HasComponent<RenderMeshArray>(lastEntity)) {
                    var materials = new Material[] {
                        EntityManager.GetComponentObject<ChunkMeshSystemData>(SystemHandle).Material
                    };

                    var meshes = new Mesh[] {
                        mesh
                    };

                    RenderMeshUtility.AddComponents(
                        lastEntity,
                        EntityManager,
                        new RenderMeshDescription(ShadowCastingMode.Off),
                        new RenderMeshArray(materials, meshes),
                        MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
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
                Mesh.ApplyAndDisposeWritableMeshData(job.MeshDataArray, mesh, MESH_UPDATE_FLAGS);
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