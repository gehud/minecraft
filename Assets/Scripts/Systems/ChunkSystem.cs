using Minecraft.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft.Systems {
	public partial class ChunkSystem : SystemBase {
		protected override void OnCreate() {
			var entity = EntityManager.CreateEntity();
			EntityManager.AddComponentData(entity, new LocalToWorld {
				Value = float4x4.identity
			});

			RenderMeshUtility.AddComponents(
				entity,
				EntityManager,
				new RenderMeshDescription(ShadowCastingMode.Off),
				new RenderMeshArray(
					new Material[1] { 
						new Material(Shader.Find("Universal Render Pipeline/Unlit")) 
					}, new Mesh[1] { 
						new Mesh() 
					}
				), 
				MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
			);
			

			EntityManager.SetComponentData(entity, new RenderBounds {
				Value = new AABB {
					Center = Vector3.one * 8.0f,
					Extents = Vector3.one * 8.0f
				}
			});

			EntityManager.SetComponentData(entity, new WorldRenderBounds {
				Value = new AABB {
					Center = Vector3.one * 8.0f,
					Extents = Vector3.one * 8.0f
				}
			});

			var voxels = new NativeArray<Voxel>(Chunk.VOLUME, Allocator.Persistent);
			for (int i = 0; i < Chunk.VOLUME; i++) {
				voxels[i] = new Voxel(1);
			}

			var claster = new NativeArray<Entity>(3 * 3 * 3, Allocator.Persistent);
			claster[9 + 3 + 1] = entity;

			EntityManager.AddComponentData(entity, new Chunk {
				Claster = claster,
				Voxels = voxels
			});

			EntityManager.AddComponent<DirtyChunk>(entity);
		}

		protected override void OnUpdate() {

		}
	}
}