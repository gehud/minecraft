using Minecraft.Utilities;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

namespace Minecraft {
	public class ChunkLoader : MonoBehaviour {
		public bool IsWorldCreated => isWorldCreated;
		private bool isWorldCreated = false;

		public float WorldLoadingProgress => worldLoadingProgress;
		private float worldLoadingProgress = 0.0f;

		public event Action OnWorldCreate;
		public event Action<Vector2> OnStartLoading;

		[SerializeField]
		private UnityEvent onWorldCreate;

		[SerializeField]
		private float loadCountdown = 0.5f;

		[Inject]
		private readonly World world;

		[Inject]
		private readonly BlockProvider blockProvider;

		[Inject]
		private readonly LightSolver lightSolver;

		[Inject]
		private readonly ChunkGenerator chunkGenerator;

		[Inject]
		private readonly SaveManager saveManager;

		private Transform player;
		private Vector2Int center;
		private readonly ConcurrentStack<Vector3Int> chunks = new();
		private readonly ConcurrentStack<Vector3Int> renderers = new();
		private readonly ConcurrentStack<Vector2Int> sunlights = new();

		private Task loading;
		private bool isLoadingCanceled = false;

		private readonly CancellationTokenSource cancellationTokenSource = new();

		public void OnPLayerSpawn(GameObject player) {
			this.player = player.transform;
		}

		private Vector2Int GetPlayerCenter() {
			if (player == null)
				return Vector2Int.zero;

			var blockCoordinate = CoordinateUtility.ToCoordinate(player.transform.position);
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

			worldLoadingProgress = 0.0f;
			float step = 1.0f / world.DrawDistance;

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
						ConcurrentDictionary<Vector3Int, ChunkUpdateJob> generatedMeshDatas = new();
						if (isLoadingCanceled)
							return;
						await Task.Run(() => { 
							GenerateLoadData(new Vector2Int(x, z));
						}, cancellationTokenSource.Token);

						foreach (var item in chunks) {
							if (isLoadingCanceled)
								return;
							Chunk chunk;
							if (saveManager.IsSaved(item))
								chunk = saveManager.LoadChunk(world, item);
							else
								chunk = await Task.Run(() => chunkGenerator.Generate(item), cancellationTokenSource.Token);
							generatedData.TryAdd(item, chunk);
							world.SetChunk(item, chunk);
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
								lightSolver.AddSunlight(item);

							lightSolver.Solve(LightMap.SUN);

							foreach (var item in renderers) {
								generatedMeshDatas.TryAdd(item, new ChunkUpdateJob(world, world.GetChunk(item), blockProvider));
							}
						}, cancellationTokenSource.Token);

						foreach (var item in generatedMeshDatas) {
							if (isLoadingCanceled)
								return;
							ChunkRenderer renderer = world.CreateRenderer(item.Key);
							renderer.Update(item.Value);
							await Task.Yield();
						}
					}
				}

				worldLoadingProgress += step;
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
					if (!blockProvider.Get(chunk.BlockMap[localBlockCoordinate]).IsSolid)
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

		private IEnumerator StartLoading() {
			var playerCenter = GetPlayerCenter();
			center = playerCenter;
			loading = Load();
			StartCoroutine(LaunchLoadingIfNeeded());
			yield return new WaitUntil(() => loading.IsCompleted);
			onWorldCreate.Invoke();
			OnWorldCreate?.Invoke();
			isWorldCreated = true;
		}

		private void OnSaveLoaded(SaveLoadData data) {
			OnStartLoading?.Invoke(data.Offset);
			StartCoroutine(StartLoading());
		}

		private void StartRelaunchLoading() {
			StartCoroutine(RelaunchLoading());
		}

		private void OnEnable() {
			world.OnDrawDistanceChanged += StartRelaunchLoading;
			saveManager.OnLoad += OnSaveLoaded;
			PlayerEvents.OnSpawn += OnPLayerSpawn;
		}

		private void OnDisable() {
			world.OnDrawDistanceChanged -= StartRelaunchLoading;
			saveManager.OnLoad -= OnSaveLoaded;
			PlayerEvents.OnSpawn -= OnPLayerSpawn;
		}

		private void OnDestroy() {
			cancellationTokenSource.Cancel();
			isLoadingCanceled = true;
		}
	}
}