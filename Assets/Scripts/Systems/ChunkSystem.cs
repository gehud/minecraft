using Minecraft.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft.Systems {
	public partial struct ChunkSystem : ISystem {
		void ISystem.OnCreate(ref SystemState state) {
			var entity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponentData(entity, new ChunkInitializer {
				Coordinate = new int3(0, 0, 0)
			});
		}

		void ISystem.OnUpdate(ref SystemState state) {
			var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
			var entities = new NativeList<Entity>(Allocator.TempJob);
			
			foreach (var (chunkInitializer, entity) in SystemAPI.Query<ChunkInitializer>().WithEntityAccess()) {
				entities.Add(entity);
				
				var position = chunkInitializer.Coordinate * Chunk.SIZE;

				entityCommandBuffer.AddComponent(entity, new LocalToWorld {
					Value = float4x4.Translate(position)
				});

				entityCommandBuffer.SetComponent(entity, new RenderBounds {
					Value = new AABB {
						Center = new float3(8.0f, 8.0f, 8.0f),
						Extents = new float3(8.0f, 8.0f, 8.0f)
					}
				});

				entityCommandBuffer.SetComponent(entity, new WorldRenderBounds {
					Value = new AABB {
						Center = new float3(8.0f, 8.0f, 8.0f) + position,
						Extents = new float3(8.0f, 8.0f, 8.0f)
					}
				});

				var voxels = new NativeArray<Voxel>(Chunk.VOLUME, Allocator.Persistent);

				var claster = new NativeArray<Entity>(3 * 3 * 3, Allocator.Persistent);
				claster[9 + 3 + 1] = entity;

				entityCommandBuffer.AddComponent(entity, new Chunk {
					Claster = claster,
					Voxels = voxels
				});

				entityCommandBuffer.AddComponent<DirtyChunk>(entity);
				entityCommandBuffer.RemoveComponent<ChunkInitializer>(entity);
			}

			foreach (var entity in entities) {
				var materials = new Material[] {
					new Material(Shader.Find("Universal Render Pipeline/Unlit"))
				};

				var meshes = new Mesh[] {
					new Mesh()
				};

				RenderMeshUtility.AddComponents(
					entity,
					state.EntityManager,
					new RenderMeshDescription(ShadowCastingMode.Off),
					new RenderMeshArray(materials, meshes),
					MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
				);
			}

			entityCommandBuffer.Playback(state.EntityManager);
			entityCommandBuffer.Dispose();
			entities.Dispose();
		}
	}
}