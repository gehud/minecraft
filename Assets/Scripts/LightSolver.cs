using Minecraft.Utilities;
using UnityEngine;
using Zenject;

namespace Minecraft {
	public class LightSolver : MonoBehaviour {
		[Inject]
		private readonly World world;

		[Inject]
		private readonly BlockProvider blockProvider;

		private readonly LightCalculator[] calculators = new LightCalculator[4];

		public void AddLight(int chanel, Vector3Int blockCoordinate, byte level) {
			calculators[(int)chanel].AddLight(blockCoordinate, level);
		}

		public void AddLight(int chanel, int x, int y, int z, byte level) {
			calculators[(int)chanel].AddLight(x, y, z, level);
		}

		public void AddLight(int chanel, Vector3Int blockCoordinate) {
			calculators[(int)chanel].AddLight(blockCoordinate);
		}

		public void AddLight(int chanel, int x, int y, int z) {
			calculators[(int)chanel].AddLight(x, y, z);
		}

		public void RemoveLight(int chanel, Vector3Int blockCoordinate) {
			calculators[(int)chanel].RemoveLight(blockCoordinate);
		}

		public void RemoveLight(int chanel, int x, int y, int z) {
			calculators[(int)chanel].RemoveLight(x, y, z);
		}

		public void AddSunlight(Vector2Int column) {
			int startX = column.x * Chunk.SIZE;
			int endX = column.x * Chunk.SIZE + Chunk.SIZE;
			int startZ = column.y * Chunk.SIZE;
			int endZ = column.y * Chunk.SIZE + Chunk.SIZE;
			for (int x = startX; x < endX; x++)
				for (int z = startZ; z < endZ; z++) {
					for (int y = World.HEIGHT * Chunk.SIZE - 1; y >= 0; y--) {
						var blockCoordinate = new Vector3Int(x, y, z);
						var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
						if (!world.TryGetChunk(chunkCoordinate, out Chunk chunk))
							break;
						var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
						if (chunk.BlockMap[localBlockCoordinate] != BlockType.Air)
							break;
						AddLight(LightMap.SUN, blockCoordinate, LightMap.MAX);
					}
				}
		}

		public void Solve(int chanel) {
			calculators[(int)chanel].Calculate();
		}

		private void Awake() {
			calculators[(int)LightMap.RED] = new LightCalculator(LightMap.RED, world, blockProvider);
			calculators[(int)LightMap.GREEN] = new LightCalculator(LightMap.GREEN, world, blockProvider);
			calculators[(int)LightMap.BLUE] = new LightCalculator(LightMap.BLUE, world, blockProvider);
			calculators[(int)LightMap.SUN] = new LightCalculator(LightMap.SUN, world, blockProvider);
		}
	}
}