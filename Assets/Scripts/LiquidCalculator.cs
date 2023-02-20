using Minecraft.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft {
	public class LiquidCalculator {
		private struct Entry {
			public Vector3Int Coordinate;
			public byte Amount;

			public Entry(Vector3Int coordinate, byte amount) {
				Coordinate = coordinate;
				Amount = amount;
			}

			public Entry(int x, int y, int z, byte amount) : this(new Vector3Int(x, y, z), amount) { }
		}

		private readonly World world;
		private readonly BlockType liquidType;
		private readonly Queue<Entry> removeQueue = new();
		private readonly Queue<Entry> addQueue = new();

		private static BlockProvider BlockDataProvider { get; set; }

		private static readonly Vector3Int[] blockSides = {
			new Vector3Int( 0,  0,  1),
			new Vector3Int( 0,  0, -1),
			new Vector3Int( 0,  1,  0),
			new Vector3Int( 0, -1,  0),
			new Vector3Int( 1,  0,  0),
			new Vector3Int(-1,  0,  0),
		};

		private static readonly Vector3Int[] flowSides = {
			new Vector3Int( 0,  0,  1),
			new Vector3Int( 0,  0, -1),
			new Vector3Int( 1,  0,  0),
			new Vector3Int(-1,  0,  0),
		};

		public static void SetBlockDataManager(BlockProvider blockDataManager) {
			BlockDataProvider = blockDataManager;
		}

		public LiquidCalculator(World world, BlockType liquidType) {
			this.world = world;
			this.liquidType = liquidType;
		}

		public void Add(Vector3Int blockCoordinate, byte amount) {
			if (amount < 1)
				return;

			var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
			if (!world.TryGetChunk(chunkCoordinate, out Chunk chunk))
				return;

			var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
			if (BlockDataProvider.Get(chunk.BlockMap[localBlockCoordinate]).IsSolid)
				return;

			chunk.LiquidMap[localBlockCoordinate] = new LiquidData(liquidType, amount);
			chunk.BlockMap[localBlockCoordinate] = liquidType;
			chunk.MarkDirty();
			world.ValidateChunk(chunkCoordinate, localBlockCoordinate);

			var entry = new Entry(blockCoordinate, amount);
			addQueue.Enqueue(entry);
		}

		public void Add(Vector3Int blockCoordinate) {
			Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
			if (!world.TryGetChunk(chunkCoordinate, out Chunk chunk))
				return;

			Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
			byte amount = chunk.LiquidMap.Get(localBlockCoordinate, liquidType);
			if (amount < 1)
				return;

			var entry = new Entry(blockCoordinate, amount);
			addQueue.Enqueue(entry);
		}

		public void Remove(Vector3Int blockCoordinate) {
			Vector3Int chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
			if (!world.TryGetChunk(chunkCoordinate, out Chunk chunk))
				return;

			Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
			byte amount = chunk.LiquidMap.Get(localBlockCoordinate, liquidType);
			if (amount < 1)
				return;

			chunk.LiquidMap[localBlockCoordinate] = LiquidData.Empty;
			chunk.BlockMap[localBlockCoordinate] = BlockType.Air;
			chunk.MarkDirty();
			world.ValidateChunk(chunkCoordinate, localBlockCoordinate);

			Entry entry = new(blockCoordinate, amount);
			removeQueue.Enqueue(entry);
		}

		private Vector3Int GetFlowDirection(Vector3Int origin) {
			foreach (var side in flowSides) {
				for (int e = 1; e <= 5; e++) {
					var x = origin.x + side.x * e;
					var y = origin.y;
					var z = origin.z + side.z * e;
					var blockCoordinate = new Vector3Int(x, y, z);
					var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
					if (world.TryGetChunk(chunkCoordinate, out Chunk chunk)) {
						var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
						if (BlockDataProvider.Get(chunk.BlockMap[localBlockCoordinate]).IsSolid)
							break;
						blockCoordinate += Vector3Int.down;
						chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
						if (world.TryGetChunk(chunkCoordinate, out chunk)) {
							localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
							if (chunk.BlockMap[localBlockCoordinate] == BlockType.Air)
								return side;
						}
					}
				}
			}

			return Vector3Int.zero;
		}

		private bool IsRenewable(Vector3Int blockCoordinate) {
			var amount000 = world.GetLiquidAmount(blockCoordinate + Vector3Int.right);
			var amount090 = world.GetLiquidAmount(blockCoordinate + Vector3Int.forward);
			var amount180 = world.GetLiquidAmount(blockCoordinate + Vector3Int.left);
			var amount270 = world.GetLiquidAmount(blockCoordinate + Vector3Int.back);
			return (amount000 == LiquidMap.MAX && amount090 == LiquidMap.MAX)
				|| (amount000 == LiquidMap.MAX && amount180 == LiquidMap.MAX)
				|| (amount000 == LiquidMap.MAX && amount270 == LiquidMap.MAX)
				|| (amount090 == LiquidMap.MAX && amount180 == LiquidMap.MAX)
				|| (amount090 == LiquidMap.MAX && amount270 == LiquidMap.MAX)
				|| (amount180 == LiquidMap.MAX && amount270 == LiquidMap.MAX);
		}

		public void Calculate() {
			var toRemove = new Queue<Entry>();
			var toAdd = new Queue<Entry>();

			while (removeQueue.TryDequeue(out Entry entry)) {
				foreach (var side in blockSides) {
					var blockCoordinate = entry.Coordinate + side;
					var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
					if (world.TryGetChunk(chunkCoordinate, out Chunk chunk)) {
						var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
						var amount = chunk.LiquidMap.Get(localBlockCoordinate, liquidType);
						if (amount != 0
							&& (amount == entry.Amount - 1 || side.y == -1 && amount == LiquidMap.MAX)
							&& !IsRenewable(blockCoordinate)) {
							var removeEntry = new Entry(blockCoordinate, amount);
							toRemove.Enqueue(removeEntry);
							chunk.LiquidMap.Set(localBlockCoordinate, liquidType, LiquidMap.MIN);
							chunk.BlockMap[localBlockCoordinate] = BlockType.Air;
							chunk.MarkDirty();
							world.ValidateChunk(chunkCoordinate, localBlockCoordinate);
						} else if (amount >= entry.Amount) {
							var addEntry = new Entry(blockCoordinate, amount);
							addQueue.Enqueue(addEntry);
						}
					}
				}
			}

			while (addQueue.TryDequeue(out Entry entry)) {
				Vector3Int blockCoordinate;
				Vector3Int chunkCoordinate;

				if (entry.Amount < 1)
					continue;

				blockCoordinate = entry.Coordinate + Vector3Int.down;
				chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
				if (world.TryGetChunk(chunkCoordinate, out Chunk chunk)) {
					Vector3Int localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
					if (BlockDataProvider.Get(chunk.BlockMap[localBlockCoordinate]).IsSolid) {
						Vector3Int flowDirection = GetFlowDirection(entry.Coordinate);
						foreach (var side in flowSides) {
							blockCoordinate = entry.Coordinate + side;
							if (flowDirection != Vector3Int.zero && flowDirection != side
								&& BlockDataProvider.Get(world.GetBlock(blockCoordinate + Vector3Int.down)).IsSolid)
								continue;
							chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
							if (world.TryGetChunk(chunkCoordinate, out chunk)) {
								localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
								var blockType = chunk.BlockMap[localBlockCoordinate];
								var amount = chunk.LiquidMap.Get(localBlockCoordinate, liquidType);
								if (!BlockDataProvider.Get(blockType).IsSolid) {
									if (amount + 2 <= entry.Amount) {
										byte newAmount = (byte)(entry.Amount - 1);
										chunk.LiquidMap.Set(localBlockCoordinate, liquidType, newAmount);
										chunk.BlockMap[localBlockCoordinate] = liquidType;
										var addEntry = new Entry(blockCoordinate, newAmount);
										toAdd.Enqueue(addEntry);
										chunk.MarkDirty();
										world.ValidateChunk(chunkCoordinate, localBlockCoordinate);
									}
								}
							}
						}
					} else {
						chunk.LiquidMap.Set(localBlockCoordinate, liquidType, LiquidMap.MAX);
						chunk.BlockMap[localBlockCoordinate] = liquidType;
						var addEntry = new Entry(blockCoordinate, LiquidMap.MAX);
						toAdd.Enqueue(addEntry);
						chunk.MarkDirty();
						world.ValidateChunk(chunkCoordinate, localBlockCoordinate);
					}
				}
			}

			for (int i = 0; i < toRemove.Count; i++)
				removeQueue.Enqueue(toRemove.Dequeue());
			for (int i = 0; i < toAdd.Count; i++)
				addQueue.Enqueue(toAdd.Dequeue());
		}
	}
}