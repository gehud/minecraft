using Minecraft.Components;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Systems {
	[UpdateAfter(typeof(ChunkBufferingSystem))]
	public partial class ChunkLoadingSystem : SystemBase {
		private SystemHandle chunkBufferingSystem;

		private int2 center;
		private readonly ConcurrentStack<int3> chunks = new();
		private readonly ConcurrentStack<int3> renderers = new();

		private Task loading;
		private bool isLoadingCanceled = false;

		private readonly CancellationTokenSource cancellationTokenSource = new();

		protected override void OnUpdate() {
			chunkBufferingSystem = World.GetExistingSystem<ChunkBufferingSystem>();
		}

		private void GenerateLoadData(int2 column) {
			var chunkBuffer = EntityManager.GetComponentData<ChunkBuffer>(chunkBufferingSystem);

			chunks.Clear();
			renderers.Clear();
			int startX = column.x - 1;
			int endX = column.x + 1;
			int startZ = column.y - 1;
			int endZ = column.y + 1;
			for (int x = startX; x <= endX; x++) {
				for (int z = startZ; z <= endZ; z++) {
					for (int y = 0; y < ChunkBuffer.HEIGHT; y++) {
						var chunkCoordinate = new int3(x, y, z);
						if (x != startX && x != endX && z != startZ && z != endZ) {
							if (!ChunkBufferingSystem.HasRenderedChunk(EntityManager, chunkBuffer, chunkCoordinate)) {
								renderers.Push(chunkCoordinate);
							}
						}

						if (!ChunkBufferingSystem.HasChunk(chunkBuffer, chunkCoordinate)) {
							chunks.Push(chunkCoordinate);
						}
					}

					if (isLoadingCanceled)
						return;
				}
			}
		}

		private async Task Load() {
			var chunkBuffer = EntityManager.GetComponentDataRW<ChunkBuffer>(chunkBufferingSystem);
			ChunkBufferingSystem.UpdateBuffer(EntityManager, ref chunkBuffer.ValueRW, center);

			for (int zone = 1; zone <= chunkBuffer.ValueRW.DrawDistance; zone++) {
				if (isLoadingCanceled) {
					return;
				}

				int startX = chunkBuffer.ValueRW.Center.x - zone;
				int endX = chunkBuffer.ValueRW.Center.x + zone;
				int startZ = chunkBuffer.ValueRW.Center.y - zone;
				int endZ = chunkBuffer.ValueRW.Center.y + zone;
				for (int x = startX; x <= endX; x++) {
					for (int z = startZ; z <= endZ; z++) {
						ConcurrentDictionary<int3, Chunk> generatedData = new();

						if (isLoadingCanceled) {
							return;
						}

						await Task.Run(() => {
							GenerateLoadData(new int2(x, z));
						}, cancellationTokenSource.Token);

						foreach (var item in chunks) {
							if (isLoadingCanceled) {
								return;
							}

							Chunk chunk = new();
							//chunk = await Task.Run(() => ChunkGenerationSystem.Generate(item), cancellationTokenSource.Token);
							generatedData.TryAdd(item, chunk);
						}

						//await Task.Run(() => {
						//	foreach (var item in sunlights)
						//		LightCalculator.AddSunlight(world, item);

						//	world.LightCalculatorSun.Calculate();

						//	foreach (var item in renderers)
						//		generatedMeshDatas.TryAdd(item, ChunkUtility.GenerateMeshData(world, world.GetChunk(item), blockDataProvider));
						//}, cancellationTokenSource.Token);

						//foreach (var item in generatedMeshDatas) {
						//	if (isLoadingCanceled)
						//		return;
						//	ChunkRenderer renderer = world.CreateRenderer(item.Key);
						//	renderer.UpdateMesh(item.Value, materialManager);
						//	await Task.Yield();
						//}
					}
				}
			}
		}
	}
}