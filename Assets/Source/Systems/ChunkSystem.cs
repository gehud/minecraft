using Minecraft.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft.Systems
{
    public partial class ChunkSystem : SystemBase {
        private void SetupChunk(NativeArray<Entity> entities) {
            if (entities.Length == 0) {
                return;
            }

            var entity = entities[0];

            var chunkInitializer = EntityManager.GetComponentData<ChunkInitializer>(entity);

            if (!chunkInitializer.HasRenderer) {
                EntityManager.AddComponent<DisableRendering>(entity);
            }

            var position = chunkInitializer.Coordinate * Chunk.Size;

            EntityManager.AddComponentData(entity, new LocalToWorld {
                Value = float4x4.Translate(position)
            });

            var materials = new Material[] {
                EntityManager.GetComponentObject<ChunkSystemData>(SystemHandle).TerrainMaterial
            };

            var mesh = new Mesh {
                name = "Empty"
            };

            var meshes = new Mesh[] {
                    mesh
                };

            RenderMeshUtility.AddComponents(
                entity,
                EntityManager,
                new RenderMeshDescription(ShadowCastingMode.Off),
                new RenderMeshArray(materials, meshes),
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
            );

            EntityManager.SetComponentData(entity, new RenderBounds {
                Value = new AABB {
                    Center = new float3(8.0f, 8.0f, 8.0f),
                    Extents = new float3(8.0f, 8.0f, 8.0f)
                }
            });

            EntityManager.SetComponentData(entity, new WorldRenderBounds {
                Value = new AABB {
                    Center = new float3(8.0f, 8.0f, 8.0f) + position,
                    Extents = new float3(8.0f, 8.0f, 8.0f)
                }
            });

            var voxels = new NativeArray<Voxel>(Chunk.Volume, Allocator.Persistent);

            EntityManager.AddComponentData(entity, new Chunk {
                Coordinate = chunkInitializer.Coordinate,
                Voxels = voxels
            });

            EntityManager.SetName(entity, $"Chunk({chunkInitializer.Coordinate.x}, {chunkInitializer.Coordinate.y}, {chunkInitializer.Coordinate.z})");
            EntityManager.AddComponent<RawChunk>(entity);
            EntityManager.RemoveComponent<ChunkInitializer>(entity);
        }

        protected override void OnUpdate() {
            var querry = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ChunkInitializer>()
                .Build(EntityManager);
            var entities = querry.ToEntityArray(Allocator.Temp);
            querry.Dispose();

            SetupChunk(entities);

            entities.Dispose();
        }
    }
}