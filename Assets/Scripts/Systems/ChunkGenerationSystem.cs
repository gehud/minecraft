using Minecraft.Components;
using Minecraft.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Minecraft.Systems {
	[BurstCompile]
	[UpdateAfter(typeof(ChunkSystem))]
	public partial struct ChunkGenerationSystem : ISystem {
		[BurstCompile]
		void ISystem.OnUpdate(ref SystemState state) {
			var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
			
			foreach (var (chunk, entity) in SystemAPI.Query<RefRW<Chunk>>().
				WithAll<RawChunk>().
				WithEntityAccess()) {

				for (int i = 0; i < Chunk.VOLUME; i++) {
					var coordinate = Array3DUtility.To3D(i, Chunk.SIZE, Chunk.SIZE);
					if (chunk.ValueRO.Coordinate.y * Chunk.SIZE + coordinate.y <= 32) {
						chunk.ValueRW.Voxels[i] = new Voxel(1);
					}
				}

				commandBuffer.RemoveComponent<RawChunk>(entity);	
				commandBuffer.AddComponent<DirtyChunk>(entity);
			}

			commandBuffer.Playback(state.EntityManager);
			commandBuffer.Dispose();
		}
	}
}