using Minecraft.Utilities;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

namespace Minecraft {
	public class ChunkLoader : MonoBehaviour {
		[SerializeField]
		private UnityEvent onWorldCreate;

		[SerializeField]
		private Transform player;
		[SerializeField]
		private float loadCountdown = 0.5f;

		[Inject]
		private readonly World world;

		[Inject]
		private readonly BlockProvider blockDataProvider;

		[Inject]
		private readonly MaterialProvider materialManager;

		[Inject]
		private readonly ChunkGenerator chunkGenerator;

		private Vector2Int center;
		private readonly ConcurrentStack<Vector3Int> chunks = new();
		private readonly ConcurrentStack<Vector3Int> renderers = new();
		private readonly ConcurrentStack<Vector2Int> sunlights = new();

		private Task loading;
		private bool isLoadingCanceled = false;

		private readonly CancellationTokenSource cancellationTokenSource = new();

		private string selectedWorld;

		private Chunk LoadSaved(Vector3Int coordinate) {
			var path = Application.persistentDataPath + "/saves/" + selectedWorld + ".world";
			using var binaryReader = new BinaryReader(File.Open(path, FileMode.Open));
			binaryReader.BaseStream.Position += sizeof(float) * 2;
			while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length) {
				int x = binaryReader.ReadInt32();
				int y = binaryReader.ReadInt32();
				int z = binaryReader.ReadInt32();
				if (new Vector3Int(x, y, z) == coordinate) {
					var result = world.CreateChunk(coordinate);
					byte[] bytes = new byte[Chunk.VOLUME * 2];
					binaryReader.Read(bytes, 0, Chunk.VOLUME * 2);
					for (int i = 0; i < Chunk.VOLUME; i++)
						result.BlockMap[i] = (BlockType)bytes[i];
					for (int i = 0; i < Chunk.VOLUME; i++)
						result.LiquidMap[i] = bytes[Chunk.VOLUME + i];
					return result;
				}
				binaryReader.BaseStream.Position += Chunk.VOLUME * 2;
			}

			throw new Exception("Failed to load chunk.");
		}

		private Vector2Int GetPlayerCenter() {
			var blockCoordinate = CoordinateUtility.ToCoordinate(player.position);
			var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
			return new Vector2Int(chunkCoordinate.x, chunkCoordinate.z);
		}

		private void GenerateLoadData(Vector2Int column) {
			chunks.Clear();
			renderers.Clear();
			sunlights.Clear();
			int startX = column.x - 1;
			int endX = column.x + 1;
			int startZ = column.y - 1;
			int endZ = column.y + 1;
			for (int x = startX; x <= endX; x++) {
				for (int z = startZ; z <= endZ; z++) {
					bool sunlight = false;
					for (int y = 0; y < World.HEIGHT; y++) {
						Vector3Int chunkCoordinate = new Vector3Int(x, y, z);
						if (x != startX && x != endX && z != startZ && z != endZ) {
							if (!world.HasRenderer(chunkCoordinate)) {
								renderers.Push(chunkCoordinate);
							}
						}

						if (!world.HasChunk(chunkCoordinate)) {
							chunks.Push(chunkCoordinate);
							sunlight = true;
						}
					}

					if (sunlight) {
						sunlights.Push(new Vector2Int(x, z));
					}

					if (isLoadingCanceled)
						return;
				}
			}
		}

		private async Task Load() {
			world.Center = center; 

			for (int zone = 1; zone <= world.DrawDistance; zone++) {
				if (isLoadingCanceled)
					return;

				int startX = world.Center.x - zone;
				int endX = world.Center.x + zone;
				int startZ = world.Center.y - zone;
				int endZ = world.Center.y + zone;
				for (int x = startX; x <= endX; x++) {
					for (int z = startZ; z <= endZ; z++) {
						ConcurrentDictionary<Vector3Int, Chunk> generatedData = new();
						ConcurrentDictionary<Vector3Int, ConcurrentDictionary<MaterialType, MeshData>> generatedMeshDatas = new();
						if (isLoadingCanceled)
							return;
						await Task.Run(() => { 
							GenerateLoadData(new Vector2Int(x, z));
						}, cancellationTokenSource.Token);

						foreach (var item in chunks) {
							if (isLoadingCanceled)
								return;
							Chunk chunk;
							if (world.Saved.Contains(item))
								chunk = LoadSaved(item);
							else
								chunk = await Task.Run(() => chunkGenerator.Generate(item), cancellationTokenSource.Token);
							generatedData.TryAdd(item, chunk);
						}

						await Task.Run(() => {
							foreach (var item in generatedData) {
								foreach (var treeRoot in item.Value.TreeData.Positions) {
									GenerateTree(CoordinateUtility.ToGlobal(item.Value.Coordinate, treeRoot));
								}
							}

							foreach (var item in renderers) {
								ChunkUtility.ParallelFor((localBlockCoordinate) => {
									Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(item, localBlockCoordinate);
									if (world.GetBlock(blockCoordinate) == BlockType.Water) {
										if (LiquidCalculator.ShouldFlow(world, blockCoordinate))
											world.LiquidCalculatorWater.Add(blockCoordinate);
									}
								});
							}

							foreach (var item in sunlights)
								LightCalculator.AddSunlight(world, item);

							world.LightCalculatorSun.Calculate();

							foreach (var item in renderers)
								generatedMeshDatas.TryAdd(item, ChunkUtility.GenerateMeshData(world, world.GetChunk(item), blockDataProvider));
						}, cancellationTokenSource.Token);

						foreach (var item in generatedMeshDatas) {
							if (isLoadingCanceled)
								return;
							ChunkRenderer renderer = world.CreateRenderer(item.Key);
							renderer.UpdateMesh(item.Value, materialManager);
							await Task.Yield();
						}
					}
				}
			}
		}

		private void GenerateTree(Vector3Int blockCoordinate) {
			for (int i = 0; i < 6; i++) {
				Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
				if (world.TryGetChunk(chunkCoordinate, out Chunk chunk)) {
					Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
					chunk.BlockMap[localBlockCoordinate] = BlockType.Log;
				}

				blockCoordinate += Vector3Int.up;
			}

			void PlaceLeaves(int x, int y, int z) {
				Vector3Int leavesCoordinate = blockCoordinate + new Vector3Int(x, y, z) + Vector3Int.down;
				Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(leavesCoordinate);
				if (world.TryGetChunk(chunkCoordinate, out Chunk chunk)) {
					Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, leavesCoordinate);
					if (!blockDataProvider.Get(chunk.BlockMap[localBlockCoordinate]).IsSolid)
						chunk.BlockMap[localBlockCoordinate] = BlockType.Leaves;
				}
			}

			for (int x = -2; x <= 2; x++) {
				for (int y = -2; y < 2; y++) {
					for (int z = -2; z <= 2; z++) {
						if (y >= 0) {
							if (x != -2 && x != 2 && z != -2 && z != 2) {
								if (y == 0) {
									PlaceLeaves(x, y, z);
								} else if (!(x == -1 && z == -1 || x == -1 && z == 1 || x == 1 && z == -1 || x == 1 && z == 1)) {
									PlaceLeaves(x, y, z);
								}
							}
						} else if (!(x == -2 && z == -2 || x == -2 && z == 2 || x == 2 && z == -2 || x == 2 && z == 2)) {
							PlaceLeaves(x, y, z);
						}
					}
				}
			}
		}

		private IEnumerator RelaunchLoading() {
			if (loading != null && !loading.IsCompleted)
				isLoadingCanceled = true;

			yield return new WaitUntil(() => loading.IsCompleted);

			isLoadingCanceled = false;
			loading = Load();
		}

		private IEnumerator LaunchLoadingIfNeeded() {
			while (true) {
				Vector2Int playerCenter = GetPlayerCenter();
				if (playerCenter != center) {
					center = playerCenter;
					yield return StartCoroutine(RelaunchLoading());
				}

				yield return new WaitForSeconds(loadCountdown);
			}
		}

		private IEnumerator Start() {
			selectedWorld = PlayerPrefs.GetString("SelectedWorld");
			var playerCenter = GetPlayerCenter();
			center = playerCenter;
			loading = Load();
			StartCoroutine(LaunchLoadingIfNeeded());
			yield return new WaitUntil(() => loading.IsCompleted);
			onWorldCreate?.Invoke();
		}

		private void StartRelaunchLoading() {
			StartCoroutine(RelaunchLoading());
		}

		private void OnEnable() {
			world.OnDrawDistanceChanged += StartRelaunchLoading;
		}

		private void OnDisable() {
			world.OnDrawDistanceChanged -= StartRelaunchLoading;
		}

		private void OnDestroy() {
			cancellationTokenSource.Cancel();
			isLoadingCanceled = true;
		}
	}
}