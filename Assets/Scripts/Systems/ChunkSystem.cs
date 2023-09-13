using Minecraft.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft.Systems {
	[BurstCompile]
	public partial struct ChunkSystem : ISystem {
		void ISystem.OnUpdate(ref SystemState state) {
			var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
			var entities = new NativeList<Entity>(Allocator.TempJob);
			
			foreach (var (chunkInitializer, entity) in SystemAPI.
				Query<ChunkInitializer>().
				WithEntityAccess()) {

				entities.Add(entity);

				if (!chunkInitializer.HasRenderer) {
					commandBuffer.AddComponent<DataOnlyChunk>(entity);
				}
				
				var position = chunkInitializer.Coordinate * Chunk.SIZE;

				commandBuffer.AddComponent(entity, new LocalToWorld {
					Value = float4x4.Translate(position)
				});

				commandBuffer.SetComponent(entity, new RenderBounds {
					Value = new AABB {
						Center = new float3(8.0f, 8.0f, 8.0f),
						Extents = new float3(8.0f, 8.0f, 8.0f)
					}
				});

				commandBuffer.SetComponent(entity, new WorldRenderBounds {
					Value = new AABB {
						Center = new float3(8.0f, 8.0f, 8.0f) + position,
						Extents = new float3(8.0f, 8.0f, 8.0f)
					}
				});

				var voxels = new NativeArray<Voxel>(Chunk.VOLUME, Allocator.Persistent);

				commandBuffer.AddComponent(entity, new Chunk {
					Coordinate = chunkInitializer.Coordinate,
					Voxels = voxels
				});

				commandBuffer.SetName(entity, $"Chunk({chunkInitializer.Coordinate.x}, {chunkInitializer.Coordinate.y}, {chunkInitializer.Coordinate.z})");
				commandBuffer.AddComponent<RawChunk>(entity);
				commandBuffer.RemoveComponent<ChunkInitializer>(entity);
			}

			foreach (var entity in entities) {
				var materials = new Material[] {
					new Material(Shader.Find("Universal Render Pipeline/Unlit"))
				};

				var mesh = new Mesh();
				mesh.name = "Empty";
				var meshes = new Mesh[] {
					mesh
				};

				RenderMeshUtility.AddComponents(
					entity,
					state.EntityManager,
					new RenderMeshDescription(ShadowCastingMode.Off),
					new RenderMeshArray(materials, meshes),
					MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
				);
			}

			commandBuffer.Playback(state.EntityManager);
			commandBuffer.Dispose();
			entities.Dispose();
		}
	}
}