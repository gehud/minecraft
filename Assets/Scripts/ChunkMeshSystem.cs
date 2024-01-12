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

        private NativeArray<VertexAttributeDescriptor> descriptors;

        [BurstCompile]
        private struct MeshJob : IJob {
            public Entity Entity;
            public ChunkMeshData MeshData;
            public Mesh.MeshDataArray MeshDataArray;
            public NativeArray<VertexAttributeDescriptor> Descriptors;

            public void Execute() {
                var mesh = MeshDataArray[0];

                mesh.SetVertexBufferParams(MeshData.Vertices.Length, Descriptors);

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

        protected override void OnCreate() {
            descriptors = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Persistent);
            descriptors[0] = new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.UInt32, 2);
        }

        protected override void OnUpdate() {
            Entities.ForEach((Entity entity, in ChunkMeshData chunkMeshData) => {
                var job = new MeshJob {
                    Entity = entity,
                    MeshData = chunkMeshData,
                    MeshDataArray = Mesh.AllocateWritableMeshData(1),
                    Descriptors = descriptors
                };

                job.Schedule().Complete();

                var mesh = new Mesh();
                Mesh.ApplyAndDisposeWritableMeshData(job.MeshDataArray, mesh, UpdateFlags);

                if (!EntityManager.HasComponent<RenderMeshArray>(entity)) {
                    var materials = new Material[] {
                        EntityManager.GetComponentObject<ChunkMeshSystemData>(SystemHandle).OpaqueMaterial,
                        EntityManager.GetComponentObject<ChunkMeshSystemData>(SystemHandle).TransparentMaterial
                    };

                    var meshes = new Mesh[] {
                        mesh,
                    };

                    RenderMeshUtility.AddComponents(
                        entity,
                        EntityManager,
                        new RenderMeshDescription(ShadowCastingMode.Off),
                        new RenderMeshArray(materials, meshes),
                        MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0, 0)
                    );

                    EntityManager.SetComponentData(entity, new RenderBounds {
                        Value = new AABB {
                            Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf),
                            Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                        }
                    });

                    var coordinate = EntityManager.GetComponentData<Chunk>(entity).Coordinate;
                    var position = coordinate * Chunk.Size;

                    EntityManager.SetComponentData(entity, new WorldRenderBounds {
                        Value = new AABB {
                            Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf) + position,
                            Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                        }
                    });

                    var rendererEntity = EntityManager.CreateEntity();
                    EntityManager.SetName(rendererEntity, $"TransparentChunk({coordinate.x}, {coordinate.y}, {coordinate.z})");

                    RenderMeshUtility.AddComponents(
                        rendererEntity,
                        EntityManager,
                        new RenderMeshDescription(ShadowCastingMode.Off),
                        EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity),
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

                    var buffer = EntityManager.AddBuffer<SubChunk>(entity);
                    buffer.Add(new SubChunk {
                        Value = rendererEntity
                    });
                } else {
                    EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity).Meshes[0] = mesh;
                }

                EntityManager.RemoveComponent<ChunkMeshData>(entity);
            }).WithStructuralChanges().Run();
        }

        protected override void OnDestroy() {
            descriptors.Dispose();
        }
    }
}