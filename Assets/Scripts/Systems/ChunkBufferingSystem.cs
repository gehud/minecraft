using Minecraft.Components;
using Minecraft.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Minecraft.Systems {
	public partial struct ChunkBufferingSystem : ISystem {
		public static int ChunkToIndex(in ChunkBuffer chunkBuffer, int3 coordinate) {
			var arrayCoordinate = new int3 {
				x = coordinate.x - chunkBuffer.Center.x + chunkBuffer.DrawDistance + 1,
				y = coordinate.y,
				z = coordinate.z - chunkBuffer.Center.y + chunkBuffer.DrawDistance + 1
			};

			return Array3DUtility.To1D(arrayCoordinate.x, arrayCoordinate.y, 
				arrayCoordinate.z, chunkBuffer.ChunksSize, ChunkBuffer.HEIGHT);
		}

		public static bool HasChunk(in ChunkBuffer chunkBuffer, int3 coordinate) {
			var index = ChunkToIndex(chunkBuffer, coordinate);
			if (index < 0 || index >= chunkBuffer.ChunksSize) {
				return false;
			}

			return chunkBuffer.Chunks[index] != Entity.Null;
		}

		public static bool HasRenderedChunk(EntityManager entityManager, in ChunkBuffer chunkBuffer, int3 coordinate) {
			var index = ChunkToIndex(chunkBuffer, coordinate);
			if (index < 0 || index >= chunkBuffer.ChunksSize) {
				return false;
			}

			var entity = chunkBuffer.Chunks[index];
			return entity != Entity.Null && !entityManager.HasComponent<DisableRendering>(entity);
		}

		public static void UpdateBuffer(EntityManager entityManager, 
			ref ChunkBuffer chunkBuffer, int2 newCenter)  {

			for (int i = 0; i < chunkBuffer.ChunksBuffer.Length; i++) {
				chunkBuffer.ChunksBuffer[i] = Entity.Null;
			}

			var centerDelta = newCenter - chunkBuffer.Center;
			for (int x = 0; x < chunkBuffer.ChunksSize; x++) {
				for (int z = 0; z < chunkBuffer.ChunksSize; z++) {
					for (int y = 0; y < ChunkBuffer.HEIGHT; y++) {
						var index = Array3DUtility.To1D(x, y, z, chunkBuffer.ChunksSize, 
							ChunkBuffer.HEIGHT);

						var chunk = chunkBuffer.Chunks[index];
						if (chunk == null)
							continue;

						int nx = x - centerDelta.x;
						int nz = z - centerDelta.y;
						if (nx < 0 || nz < 0 || nx >= chunkBuffer.ChunksSize || 
							nz >= chunkBuffer.ChunksSize) {

							entityManager.DestroyEntity(chunkBuffer.Chunks[index]);
							chunkBuffer.Chunks[index] = Entity.Null;
							continue;
						}

						chunkBuffer.ChunksBuffer[Array3DUtility.To1D(nx, y, nz, 
							chunkBuffer.ChunksSize, ChunkBuffer.HEIGHT)] = chunk;
					}
				}
			}

			(chunkBuffer.Chunks, chunkBuffer.ChunksBuffer) = 
				(chunkBuffer.ChunksBuffer, chunkBuffer.Chunks);

			chunkBuffer.Center = newCenter;
		}

		void ISystem.OnCreate(ref SystemState state) {
			var center = int2.zero;
			var drawDistance = 2;
			var chunksSize = drawDistance * 2 + 3;
			var chunksVolume = chunksSize * chunksSize * ChunkBuffer.HEIGHT;
			var chunks = new NativeArray<Entity>(chunksVolume, Allocator.Persistent);
			var chunksBuffer = new NativeArray<Entity>(chunksVolume, Allocator.Persistent);
			state.EntityManager.AddComponentData(state.SystemHandle, new ChunkBuffer {
				Center = center,
				DrawDistance = drawDistance,
				ChunksSize = chunksSize,
				Chunks = chunks,
				ChunksBuffer = chunksBuffer
			});
		}
	}
}