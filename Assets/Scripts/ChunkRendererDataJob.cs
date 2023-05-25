using Minecraft.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace Minecraft {
	public struct ChunkRendererDataJob {
		public List<Vertex> OpaqueVertices;
		public List<ushort> OpaqueIndices;
		public List<Vertex> TransparentVertices;
		public List<ushort> TransparentIndices;

		private readonly World world;
		private readonly Chunk chunk;
		private readonly BlockProvider blockProvider;

		public ChunkRendererDataJob(World world, Chunk chunk, BlockProvider blockProvider) {
			OpaqueVertices = new();
			OpaqueIndices = new();
			TransparentVertices = new();
			TransparentIndices = new();
			this.world = world;
			this.chunk = chunk;
			this.blockProvider = blockProvider;
			Parallel.For(0, Chunk.VOLUME, Execute);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AddOpaqueFaceIndices(float aof1, float aof2, float aof3, float aof4, bool force = false) {
			int vertexCount = OpaqueVertices.Count;
			if (force || aof1 + aof3 < aof2 + aof4) {
				// Fliped quad.
				OpaqueIndices.Add((ushort)(0 + vertexCount));
				OpaqueIndices.Add((ushort)(1 + vertexCount));
				OpaqueIndices.Add((ushort)(3 + vertexCount));
				OpaqueIndices.Add((ushort)(3 + vertexCount));
				OpaqueIndices.Add((ushort)(1 + vertexCount));
				OpaqueIndices.Add((ushort)(2 + vertexCount));
			} else {
				// Normal quad.
				OpaqueIndices.Add((ushort)(0 + vertexCount));
				OpaqueIndices.Add((ushort)(1 + vertexCount));
				OpaqueIndices.Add((ushort)(2 + vertexCount));
				OpaqueIndices.Add((ushort)(0 + vertexCount));
				OpaqueIndices.Add((ushort)(2 + vertexCount));
				OpaqueIndices.Add((ushort)(3 + vertexCount));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AddTransparentFaceIndices(float aof1, float aof2, float aof3, float aof4, bool force = false) {
			int vertexCount = TransparentVertices.Count;
			if (force || aof1 + aof3 < aof2 + aof4) {
				// Fliped quad.
				TransparentIndices.Add((ushort)(0 + vertexCount));
				TransparentIndices.Add((ushort)(1 + vertexCount));
				TransparentIndices.Add((ushort)(3 + vertexCount));
				TransparentIndices.Add((ushort)(3 + vertexCount));
				TransparentIndices.Add((ushort)(1 + vertexCount));
				TransparentIndices.Add((ushort)(2 + vertexCount));
			} else {
				// Normal quad.
				TransparentIndices.Add((ushort)(0 + vertexCount));
				TransparentIndices.Add((ushort)(1 + vertexCount));
				TransparentIndices.Add((ushort)(2 + vertexCount));
				TransparentIndices.Add((ushort)(0 + vertexCount));
				TransparentIndices.Add((ushort)(2 + vertexCount));
				TransparentIndices.Add((ushort)(3 + vertexCount));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		BlockType GetBlock(int x, int y, int z) {
			var blockCoordinate = CoordinateUtility.ToGlobal(chunk.Coordinate, new Vector3Int(x, y, z));
			var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
			var neighbour = world.GetChunk(chunkCoordinate);
			if (neighbour != null) {
				var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
				return neighbour.BlockMap[localBlockCoordinate];
			}

			return BlockType.Air;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int GetLight(int x, int y, int z, int chanel) {
			Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(chunk.Coordinate, new Vector3Int(x, y, z));
			var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
			var neighbour = world.GetChunk(chunkCoordinate);
			if (neighbour != null) {
				var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
				return neighbour.LightMap.Get(localBlockCoordinate, chanel);
			}

			return LightMap.MAX;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		byte GetLiquidAmount(int x, int y, int z, BlockType liquidType) {
			Vector3Int blockCoordinate = CoordinateUtility.ToGlobal(chunk.Coordinate, new Vector3Int(x, y, z));
			var chunkCoordinate = CoordinateUtility.ToChunk(blockCoordinate);
			var neighbour = world.GetChunk(chunkCoordinate);
			if (neighbour != null) {
				var localBlockCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, blockCoordinate);
				if (neighbour.BlockMap[localBlockCoordinate] == liquidType)
					return neighbour.LiquidMap[localBlockCoordinate];
				return LiquidMap.MIN;
			}

			return LiquidMap.MIN;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IsSolid(int x, int y, int z) {
			return blockProvider.Get(GetBlock(x, y, z)).IsSolid;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IsLiquid(int x, int y, int z) {
			return blockProvider.Get(GetBlock(x, y, z)).IsLiquid;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IsVoxelTransparent(int x, int y, int z) {
			return blockProvider.Get(GetBlock(x, y, z)).IsTransparent;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool HasFace(int x, int y, int z, bool isLiquid, BlockType blockType, Vector3Int localBlockCoordinate) {
			return isLiquid && (GetBlock(x, y, z) != blockType && (IsVoxelTransparent(x, y, z) || y - localBlockCoordinate.y == 1)) || (IsVoxelTransparent(x, y, z) && GetBlock(x, y, z) != blockType);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe Vertex Pack(float x, float y, float z, float u, float v, float r, float g, float b, float s, int flow) {
			int d = (Mathf.FloatToHalf(y) << 16) | (flow & 0xFFFF);
			int rg = Mathf.FloatToHalf(r) << 16 | Mathf.FloatToHalf(g);
			int bs = Mathf.FloatToHalf(b) << 16 | Mathf.FloatToHalf(s);
			return new Vertex(x, z, u, v, *(float*)&rg, *(float*)&bs, *(float*)&d);
		}

		private static readonly object lockObject = new();

		private const int SIZE_2 = Chunk.SIZE * Chunk.SIZE;

		private unsafe void Execute(int index) {
			int z = index / SIZE_2;
			index -= z * SIZE_2;
			int y = index / Chunk.SIZE;
			int x = index % Chunk.SIZE;

			BlockType blockType = chunk.BlockMap[x, y, z];

			if (blockProvider.Get(blockType).IsVegetation) {
				var blockData = blockProvider.Get(blockType);
				var atlasStep = 16.0f / 256.0f;

				Vector2 atlasPosition = (Vector2)blockData.TexturingData.FrontFace * atlasStep;
				float r = GetLight(x, y, z, LightMap.RED) / 16.0f;
				float g = GetLight(x, y, z, LightMap.GREEN) / 16.0f;
				float b = GetLight(x, y, z, LightMap.BLUE) / 16.0f;
				float s = GetLight(x, y, z, LightMap.SUN) / 16.0f;

				lock (lockObject) {
					var vertexCount = TransparentVertices.Count;
					TransparentIndices.Add((ushort)(vertexCount + 0));
					TransparentIndices.Add((ushort)(vertexCount + 1));
					TransparentIndices.Add((ushort)(vertexCount + 2));
					TransparentIndices.Add((ushort)(vertexCount + 0));
					TransparentIndices.Add((ushort)(vertexCount + 2));
					TransparentIndices.Add((ushort)(vertexCount + 3));
					TransparentVertices.Add(Pack(x + 0.0f, y + 0.0f, z + 0.0f, atlasPosition.x + 0.0f * atlasStep, atlasPosition.y + 0.0f * atlasStep, r, g, b, s, 0));
					TransparentVertices.Add(Pack(x + 0.0f, y + 1.0f, z + 0.0f, atlasPosition.x + 0.0f * atlasStep, atlasPosition.y + 1.0f * atlasStep, r, g, b, s, 0));
					TransparentVertices.Add(Pack(x + 1.0f, y + 1.0f, z + 1.0f, atlasPosition.x + 1.0f * atlasStep, atlasPosition.y + 1.0f * atlasStep, r, g, b, s, 0));
					TransparentVertices.Add(Pack(x + 1.0f, y + 0.0f, z + 1.0f, atlasPosition.x + 1.0f * atlasStep, atlasPosition.y + 0.0f * atlasStep, r, g, b, s, 0));
					TransparentIndices.Add((ushort)(vertexCount + 4));
					TransparentIndices.Add((ushort)(vertexCount + 5));
					TransparentIndices.Add((ushort)(vertexCount + 6));
					TransparentIndices.Add((ushort)(vertexCount + 4));
					TransparentIndices.Add((ushort)(vertexCount + 6));
					TransparentIndices.Add((ushort)(vertexCount + 7));
					TransparentVertices.Add(Pack(x + 0.0f, y + 0.0f, z + 1.0f, atlasPosition.x + 0.0f * atlasStep, atlasPosition.y + 0.0f * atlasStep, r, g, b, s, 0));
					TransparentVertices.Add(Pack(x + 0.0f, y + 1.0f, z + 1.0f, atlasPosition.x + 0.0f * atlasStep, atlasPosition.y + 1.0f * atlasStep, r, g, b, s, 0));
					TransparentVertices.Add(Pack(x + 1.0f, y + 1.0f, z + 0.0f, atlasPosition.x + 1.0f * atlasStep, atlasPosition.y + 1.0f * atlasStep, r, g, b, s, 0));
					TransparentVertices.Add(Pack(x + 1.0f, y + 0.0f, z + 0.0f, atlasPosition.x + 1.0f * atlasStep, atlasPosition.y + 0.0f * atlasStep, r, g, b, s, 0));
				}
			} else if (blockType != BlockType.Air) {
				var localBlockCoordinate = new Vector3Int(x, y, z);
				var blockData = blockProvider.Get(blockType);
				bool isLiquid = blockData.IsLiquid;
				bool isTwoSided = blockData.IsTwoSided;
				float atlasStep = 16.0f / 256.0f;

				byte aown = GetLiquidAmount(x + 0, y + 0, z + 0, blockType);
				byte atop = GetLiquidAmount(x + 0, y + 1, z + 0, blockType);
				byte a000 = GetLiquidAmount(x + 1, y + 0, z + 0, blockType);
				byte a045 = GetLiquidAmount(x + 1, y + 0, z + 1, blockType);
				byte a090 = GetLiquidAmount(x + 0, y + 0, z + 1, blockType);
				byte a135 = GetLiquidAmount(x - 1, y + 0, z + 1, blockType);
				byte a180 = GetLiquidAmount(x - 1, y + 0, z + 0, blockType);
				byte a225 = GetLiquidAmount(x - 1, y + 0, z - 1, blockType);
				byte a270 = GetLiquidAmount(x + 0, y + 0, z - 1, blockType);
				byte a315 = GetLiquidAmount(x + 1, y + 0, z - 1, blockType);

				bool s000 = IsSolid(x + 1, y + 0, z + 0);
				bool s045 = IsSolid(x + 1, y + 0, z + 1);
				bool s090 = IsSolid(x + 0, y + 0, z + 1);
				bool s135 = IsSolid(x - 1, y + 0, z + 1);
				bool s180 = IsSolid(x - 1, y + 0, z + 0);
				bool s225 = IsSolid(x - 1, y + 0, z - 1);
				bool s270 = IsSolid(x + 0, y + 0, z - 1);
				bool s315 = IsSolid(x + 1, y + 0, z - 1);

				byte sb000 = *(byte*)&s000;
				byte sb045 = *(byte*)&s045;
				byte sb090 = *(byte*)&s090;
				byte sb135 = *(byte*)&s135;
				byte sb180 = *(byte*)&s180;
				byte sb225 = *(byte*)&s225;
				byte sb270 = *(byte*)&s270;
				byte sb315 = *(byte*)&s315;

				byte nsb000 = (byte)(~sb000 & 0b1);
				byte nsb045 = (byte)(~sb045 & 0b1);
				byte nsb090 = (byte)(~sb090 & 0b1);
				byte nsb135 = (byte)(~sb135 & 0b1);
				byte nsb180 = (byte)(~sb180 & 0b1);
				byte nsb225 = (byte)(~sb225 & 0b1);
				byte nsb270 = (byte)(~sb270 & 0b1);
				byte nsb315 = (byte)(~sb315 & 0b1);

				bool lqt000 = IsLiquid(x + 1, y + 1, z + 0);
				bool lqt045 = IsLiquid(x + 1, y + 1, z + 1);
				bool lqt090 = IsLiquid(x + 0, y + 1, z + 1);
				bool lqt135 = IsLiquid(x - 1, y + 1, z + 1);
				bool lqt180 = IsLiquid(x - 1, y + 1, z + 0);
				bool lqt225 = IsLiquid(x - 1, y + 1, z - 1);
				bool lqt270 = IsLiquid(x + 0, y + 1, z - 1);
				bool lqt315 = IsLiquid(x + 1, y + 1, z - 1);

				bool lq000 = IsLiquid(x + 1, y + 0, z + 0);
				bool lq045 = IsLiquid(x + 1, y + 0, z + 1);
				bool lq090 = IsLiquid(x + 0, y + 0, z + 1);
				bool lq135 = IsLiquid(x - 1, y + 0, z + 1);
				bool lq180 = IsLiquid(x - 1, y + 0, z + 0);
				bool lq225 = IsLiquid(x - 1, y + 0, z - 1);
				bool lq270 = IsLiquid(x + 0, y + 0, z - 1);
				bool lq315 = IsLiquid(x + 1, y + 0, z - 1);

				byte lqb000 = *(byte*)&lq000;
				byte lqb045 = *(byte*)&lq045;
				byte lqb090 = *(byte*)&lq090;
				byte lqb135 = *(byte*)&lq135;
				byte lqb180 = *(byte*)&lq180;
				byte lqb225 = *(byte*)&lq225;
				byte lqb270 = *(byte*)&lq270;
				byte lqb315 = *(byte*)&lq315;

				byte nlqb000 = (byte)(~(lqb000 & 0b1));
				byte nlqb045 = (byte)(~(lqb045 & 0b1));
				byte nlqb090 = (byte)(~(lqb090 & 0b1));
				byte nlqb135 = (byte)(~(lqb135 & 0b1));
				byte nlqb180 = (byte)(~(lqb180 & 0b1));
				byte nlqb225 = (byte)(~(lqb225 & 0b1));
				byte nlqb270 = (byte)(~(lqb270 & 0b1));
				byte nlqb315 = (byte)(~(lqb315 & 0b1));

				// Right face.
				if (HasFace(x + 1, y, z, isLiquid, blockType, localBlockCoordinate)) {
					Vector2 atlasPosition = (Vector2)blockData.TexturingData.RightFace * atlasStep;

					bool t000 = !IsVoxelTransparent(x + 1, y + 0, z + 1);
					bool t090 = !IsVoxelTransparent(x + 1, y + 1, z + 0);
					bool t180 = !IsVoxelTransparent(x + 1, y + 0, z - 1);
					bool t270 = !IsVoxelTransparent(x + 1, y - 1, z + 0);

					int lrtop = GetLight(x + 1, y + 0, z + 0, LightMap.RED);
					int lr000 = GetLight(x + 1, y + 0, z + 1, LightMap.RED);
					int lr045 = GetLight(x + 1, y + 1, z + 1, LightMap.RED);
					int lr090 = GetLight(x + 1, y + 1, z + 0, LightMap.RED);
					int lr135 = GetLight(x + 1, y + 1, z - 1, LightMap.RED);
					int lr180 = GetLight(x + 1, y + 0, z - 1, LightMap.RED);
					int lr225 = GetLight(x + 1, y - 1, z - 1, LightMap.RED);
					int lr270 = GetLight(x + 1, y - 1, z + 0, LightMap.RED);
					int lr315 = GetLight(x + 1, y - 1, z + 1, LightMap.RED);

					float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 16.0f;
					float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 16.0f;
					float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 16.0f;
					float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 16.0f;

					int lgtop = GetLight(x + 1, y + 0, z + 0, LightMap.GREEN);
					int lg000 = GetLight(x + 1, y + 0, z + 1, LightMap.GREEN);
					int lg045 = GetLight(x + 1, y + 1, z + 1, LightMap.GREEN);
					int lg090 = GetLight(x + 1, y + 1, z + 0, LightMap.GREEN);
					int lg135 = GetLight(x + 1, y + 1, z - 1, LightMap.GREEN);
					int lg180 = GetLight(x + 1, y + 0, z - 1, LightMap.GREEN);
					int lg225 = GetLight(x + 1, y - 1, z - 1, LightMap.GREEN);
					int lg270 = GetLight(x + 1, y - 1, z + 0, LightMap.GREEN);
					int lg315 = GetLight(x + 1, y - 1, z + 1, LightMap.GREEN);

					float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 16.0f;
					float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 16.0f;
					float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 16.0f;
					float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 16.0f;

					int lbtop = GetLight(x + 1, y + 0, z + 0, LightMap.BLUE);
					int lb000 = GetLight(x + 1, y + 0, z + 1, LightMap.BLUE);
					int lb045 = GetLight(x + 1, y + 1, z + 1, LightMap.BLUE);
					int lb090 = GetLight(x + 1, y + 1, z + 0, LightMap.BLUE);
					int lb135 = GetLight(x + 1, y + 1, z - 1, LightMap.BLUE);
					int lb180 = GetLight(x + 1, y + 0, z - 1, LightMap.BLUE);
					int lb225 = GetLight(x + 1, y - 1, z - 1, LightMap.BLUE);
					int lb270 = GetLight(x + 1, y - 1, z + 0, LightMap.BLUE);
					int lb315 = GetLight(x + 1, y - 1, z + 1, LightMap.BLUE);

					float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 16.0f;
					float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 16.0f;
					float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 16.0f;
					float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 16.0f;

					int lstop = GetLight(x + 1, y + 0, z + 0, LightMap.SUN);
					int ls000 = GetLight(x + 1, y + 0, z + 1, LightMap.SUN);
					int ls045 = GetLight(x + 1, y + 1, z + 1, LightMap.SUN);
					int ls090 = GetLight(x + 1, y + 1, z + 0, LightMap.SUN);
					int ls135 = GetLight(x + 1, y + 1, z - 1, LightMap.SUN);
					int ls180 = GetLight(x + 1, y + 0, z - 1, LightMap.SUN);
					int ls225 = GetLight(x + 1, y - 1, z - 1, LightMap.SUN);
					int ls270 = GetLight(x + 1, y - 1, z + 0, LightMap.SUN);
					int ls315 = GetLight(x + 1, y - 1, z + 1, LightMap.SUN);

					float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 16.0f;
					float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 16.0f;
					float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 16.0f;
					float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 16.0f;

					float h1 = 0.0f;
					float h2 = 1.0f;
					float h3 = 1.0f;
					float h4 = 0.0f;

					int dir = 0;

					if (isLiquid) {
						if (atop == 0) {
							h2 = lqt000 || lqt270 || lqt315 ? 1.0f : (aown + a000 + a270 + a315)
								/ (float)(1 + Convert.ToInt32(!s000) + Convert.ToInt32(!s270) + Convert.ToInt32(!s315))
									/ (LiquidMap.MAX + 1);
							h3 = lqt000 || lqt045 || lqt090 ? 1.0f : (aown + a000 + a045 + a090)
								/ (float)(1 + Convert.ToInt32(!s000) + Convert.ToInt32(!s045) + Convert.ToInt32(!s090))
									/ (LiquidMap.MAX + 1);
						}
						if (a000 != 0) {
							h1 = (aown + a000 + a270 + a315)
								/ (float)(1 + Convert.ToInt32(!s000) + Convert.ToInt32(!s270) + Convert.ToInt32(!s315))
									/ (LiquidMap.MAX + 1);
							h4 = (aown + a000 + a045 + a090)
								/ (float)(1 + Convert.ToInt32(!s000) + Convert.ToInt32(!s045) + Convert.ToInt32(!s090))
									/ (LiquidMap.MAX + 1);
						}

						dir = 5;
					}

					var uv1 = new Vector2(atlasPosition.x + 0 * atlasStep, atlasPosition.y + h1 * atlasStep);
					var uv2 = new Vector2(atlasPosition.x + 0 * atlasStep, atlasPosition.y + h2 * atlasStep);
					var uv3 = new Vector2(atlasPosition.x + 1 * atlasStep, atlasPosition.y + h3 * atlasStep);
					var uv4 = new Vector2(atlasPosition.x + 1 * atlasStep, atlasPosition.y + h4 * atlasStep);

					float aof1 = lr1 + lg1 + lb1 + ls1;
					float aof2 = lr2 + lg2 + lb2 + ls2;
					float aof3 = lr3 + lg3 + lb3 + ls3;
					float aof4 = lr4 + lg4 + lb4 + ls4;

					lock (lockObject) {
						if (isTwoSided) {
							AddTransparentFaceIndices(aof1, aof2, aof3, aof4);
							TransparentVertices.Add(Pack(x + 1, y + h1, z + 0, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
							TransparentVertices.Add(Pack(x + 1, y + h2, z + 0, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
							TransparentVertices.Add(Pack(x + 1, y + h3, z + 1, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
							TransparentVertices.Add(Pack(x + 1, y + h4, z + 1, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));
						} else {
							AddOpaqueFaceIndices(aof1, aof2, aof3, aof4);
							OpaqueVertices.Add(Pack(x + 1, y + h1, z + 0, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
							OpaqueVertices.Add(Pack(x + 1, y + h2, z + 0, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
							OpaqueVertices.Add(Pack(x + 1, y + h3, z + 1, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
							OpaqueVertices.Add(Pack(x + 1, y + h4, z + 1, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));
						}
					}
				}

				// Left face.
				if (HasFace(x - 1, y, z, isLiquid, blockType, localBlockCoordinate)) {
					Vector2 atlasPosition = (Vector2)blockData.TexturingData.LeftFace * atlasStep;

					bool t000 = !IsVoxelTransparent(x - 1, y + 0, z - 1);
					bool t090 = !IsVoxelTransparent(x - 1, y + 1, z + 0);
					bool t180 = !IsVoxelTransparent(x - 1, y + 0, z + 1);
					bool t270 = !IsVoxelTransparent(x - 1, y - 1, z + 0);

					int lrtop = GetLight(x - 1, y + 0, z + 0, LightMap.RED);
					int lr000 = GetLight(x - 1, y + 0, z - 1, LightMap.RED);
					int lr045 = GetLight(x - 1, y + 1, z - 1, LightMap.RED);
					int lr090 = GetLight(x - 1, y + 1, z + 0, LightMap.RED);
					int lr135 = GetLight(x - 1, y + 1, z + 1, LightMap.RED);
					int lr180 = GetLight(x - 1, y + 0, z + 1, LightMap.RED);
					int lr225 = GetLight(x - 1, y - 1, z + 1, LightMap.RED);
					int lr270 = GetLight(x - 1, y - 1, z + 0, LightMap.RED);
					int lr315 = GetLight(x - 1, y - 1, z - 1, LightMap.RED);

					float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 16.0f;
					float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 16.0f;
					float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 16.0f;
					float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 16.0f;

					int lgtop = GetLight(x - 1, y + 0, z + 0, LightMap.GREEN);
					int lg000 = GetLight(x - 1, y + 0, z - 1, LightMap.GREEN);
					int lg045 = GetLight(x - 1, y + 1, z - 1, LightMap.GREEN);
					int lg090 = GetLight(x - 1, y + 1, z + 0, LightMap.GREEN);
					int lg135 = GetLight(x - 1, y + 1, z + 1, LightMap.GREEN);
					int lg180 = GetLight(x - 1, y + 0, z + 1, LightMap.GREEN);
					int lg225 = GetLight(x - 1, y - 1, z + 1, LightMap.GREEN);
					int lg270 = GetLight(x - 1, y - 1, z + 0, LightMap.GREEN);
					int lg315 = GetLight(x - 1, y - 1, z - 1, LightMap.GREEN);

					float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 16.0f;
					float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 16.0f;
					float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 16.0f;
					float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 16.0f;

					int lbtop = GetLight(x - 1, y + 0, z + 0, LightMap.BLUE);
					int lb000 = GetLight(x - 1, y + 0, z - 1, LightMap.BLUE);
					int lb045 = GetLight(x - 1, y + 1, z - 1, LightMap.BLUE);
					int lb090 = GetLight(x - 1, y + 1, z + 0, LightMap.BLUE);
					int lb135 = GetLight(x - 1, y + 1, z + 1, LightMap.BLUE);
					int lb180 = GetLight(x - 1, y + 0, z + 1, LightMap.BLUE);
					int lb225 = GetLight(x - 1, y - 1, z + 1, LightMap.BLUE);
					int lb270 = GetLight(x - 1, y - 1, z + 0, LightMap.BLUE);
					int lb315 = GetLight(x - 1, y - 1, z - 1, LightMap.BLUE);

					float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 16.0f;
					float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 16.0f;
					float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 16.0f;
					float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 16.0f;

					int lstop = GetLight(x - 1, y + 0, z + 0, LightMap.SUN);
					int ls000 = GetLight(x - 1, y + 0, z - 1, LightMap.SUN);
					int ls045 = GetLight(x - 1, y + 1, z - 1, LightMap.SUN);
					int ls090 = GetLight(x - 1, y + 1, z + 0, LightMap.SUN);
					int ls135 = GetLight(x - 1, y + 1, z + 1, LightMap.SUN);
					int ls180 = GetLight(x - 1, y + 0, z + 1, LightMap.SUN);
					int ls225 = GetLight(x - 1, y - 1, z + 1, LightMap.SUN);
					int ls270 = GetLight(x - 1, y - 1, z + 0, LightMap.SUN);
					int ls315 = GetLight(x - 1, y - 1, z - 1, LightMap.SUN);

					float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 16.0f;
					float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 16.0f;
					float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 16.0f;
					float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 16.0f;

					float h1 = 0.0f;
					float h2 = 1.0f;
					float h3 = 1.0f;
					float h4 = 0.0f;

					int dir = 0;

					if (isLiquid) {
						if (atop == 0) {
							h2 = lqt090 || lqt135 || lqt180 ? 1.0f : (aown + a090 + a135 + a180)
								/ (float)(1 + Convert.ToInt32(!s090) + Convert.ToInt32(!s135) + Convert.ToInt32(!s180))
									/ (LiquidMap.MAX + 1);
							h3 = lqt180 || lqt225 || lqt270 ? 1.0f : (aown + a180 + a225 + a270)
								/ (float)(1 + Convert.ToInt32(!s180) + Convert.ToInt32(!s225) + Convert.ToInt32(!s270))
									/ (LiquidMap.MAX + 1);
						}
						if (a180 != 0) {
							h1 = (aown + a090 + a135 + a180)
								/ (float)(1 + Convert.ToInt32(!s090) + Convert.ToInt32(!s135) + Convert.ToInt32(!s180))
									/ (LiquidMap.MAX + 1);
							h4 = (aown + a180 + +a270)
								/ (float)(1 + Convert.ToInt32(!s180) + Convert.ToInt32(!s225) + Convert.ToInt32(!s270))
									/ (LiquidMap.MAX + 1);
						}

						dir = 5;
					}

					var uv1 = new Vector2(atlasPosition.x + 0.0f * atlasStep, atlasPosition.y + h1 * atlasStep);
					var uv2 = new Vector2(atlasPosition.x + 0.0f * atlasStep, atlasPosition.y + h2 * atlasStep);
					var uv3 = new Vector2(atlasPosition.x + 1.0f * atlasStep, atlasPosition.y + h3 * atlasStep);
					var uv4 = new Vector2(atlasPosition.x + 1.0f * atlasStep, atlasPosition.y + h4 * atlasStep);

					float aof1 = lr1 + lg1 + lb1 + ls1;
					float aof2 = lr2 + lg2 + lb2 + ls2;
					float aof3 = lr3 + lg3 + lb3 + ls3;
					float aof4 = lr4 + lg4 + lb4 + ls4;

					lock (lockObject) {
						if (isTwoSided) {
							AddTransparentFaceIndices(aof1, aof2, aof3, aof4);
							TransparentVertices.Add(Pack(x + 0, y + h1, z + 1, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
							TransparentVertices.Add(Pack(x + 0, y + h2, z + 1, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
							TransparentVertices.Add(Pack(x + 0, y + h3, z + 0, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
							TransparentVertices.Add(Pack(x + 0, y + h4, z + 0, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));
						} else {
							AddOpaqueFaceIndices(aof1, aof2, aof3, aof4);
							OpaqueVertices.Add(Pack(x + 0, y + h1, z + 1, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
							OpaqueVertices.Add(Pack(x + 0, y + h2, z + 1, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
							OpaqueVertices.Add(Pack(x + 0, y + h3, z + 0, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
							OpaqueVertices.Add(Pack(x + 0, y + h4, z + 0, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));
						}
					}
				}

				// Top face.
				if (HasFace(x, y + 1, z, isLiquid, blockType, localBlockCoordinate)) {
					var atlasPosition = (Vector2)blockData.TexturingData.TopFace * atlasStep;

					bool t000 = !IsVoxelTransparent(x + 1, y + 1, z + 0);
					bool t090 = !IsVoxelTransparent(x + 0, y + 1, z + 1);
					bool t180 = !IsVoxelTransparent(x - 1, y + 1, z + 0);
					bool t270 = !IsVoxelTransparent(x + 0, y + 1, z - 1);

					int lrtop = GetLight(x + 0, y + 1, z + 0, LightMap.RED);
					int lr000 = GetLight(x + 1, y + 1, z + 0, LightMap.RED);
					int lr045 = GetLight(x + 1, y + 1, z + 1, LightMap.RED);
					int lr090 = GetLight(x + 0, y + 1, z + 1, LightMap.RED);
					int lr135 = GetLight(x - 1, y + 1, z + 1, LightMap.RED);
					int lr180 = GetLight(x - 1, y + 1, z + 0, LightMap.RED);
					int lr225 = GetLight(x - 1, y + 1, z - 1, LightMap.RED);
					int lr270 = GetLight(x + 0, y + 1, z - 1, LightMap.RED);
					int lr315 = GetLight(x + 1, y + 1, z - 1, LightMap.RED);

					float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 16.0f;
					float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 16.0f;
					float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 16.0f;
					float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 16.0f;

					int lgtop = GetLight(x + 0, y + 1, z + 0, LightMap.GREEN);
					int lg000 = GetLight(x + 1, y + 1, z + 0, LightMap.GREEN);
					int lg045 = GetLight(x + 1, y + 1, z + 1, LightMap.GREEN);
					int lg090 = GetLight(x + 0, y + 1, z + 1, LightMap.GREEN);
					int lg135 = GetLight(x - 1, y + 1, z + 1, LightMap.GREEN);
					int lg180 = GetLight(x - 1, y + 1, z + 0, LightMap.GREEN);
					int lg225 = GetLight(x - 1, y + 1, z - 1, LightMap.GREEN);
					int lg270 = GetLight(x + 0, y + 1, z - 1, LightMap.GREEN);
					int lg315 = GetLight(x + 1, y + 1, z - 1, LightMap.GREEN);

					float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 16.0f;
					float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 16.0f;
					float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 16.0f;
					float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 16.0f;

					int lbtop = GetLight(x + 0, y + 1, z + 0, LightMap.BLUE);
					int lb000 = GetLight(x + 1, y + 1, z + 0, LightMap.BLUE);
					int lb045 = GetLight(x + 1, y + 1, z + 1, LightMap.BLUE);
					int lb090 = GetLight(x + 0, y + 1, z + 1, LightMap.BLUE);
					int lb135 = GetLight(x - 1, y + 1, z + 1, LightMap.BLUE);
					int lb180 = GetLight(x - 1, y + 1, z + 0, LightMap.BLUE);
					int lb225 = GetLight(x - 1, y + 1, z - 1, LightMap.BLUE);
					int lb270 = GetLight(x + 0, y + 1, z - 1, LightMap.BLUE);
					int lb315 = GetLight(x + 1, y + 1, z - 1, LightMap.BLUE);

					float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 16.0f;
					float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 16.0f;
					float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 16.0f;
					float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 16.0f;

					int lstop = GetLight(x + 0, y + 1, z + 0, LightMap.SUN);
					int ls000 = GetLight(x + 1, y + 1, z + 0, LightMap.SUN);
					int ls045 = GetLight(x + 1, y + 1, z + 1, LightMap.SUN);
					int ls090 = GetLight(x + 0, y + 1, z + 1, LightMap.SUN);
					int ls135 = GetLight(x - 1, y + 1, z + 1, LightMap.SUN);
					int ls180 = GetLight(x - 1, y + 1, z + 0, LightMap.SUN);
					int ls225 = GetLight(x - 1, y + 1, z - 1, LightMap.SUN);
					int ls270 = GetLight(x + 0, y + 1, z - 1, LightMap.SUN);
					int ls315 = GetLight(x + 1, y + 1, z - 1, LightMap.SUN);

					float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 16.0f;
					float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 16.0f;
					float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 16.0f;
					float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 16.0f;

					float h1 = 1.0f;
					float h2 = 1.0f;
					float h3 = 1.0f;
					float h4 = 1.0f;

					int dir = 0;

					if (isLiquid) {
						if (atop == 0) {
							h1 = lqt180 || lqt225 || lqt270 ? 1.0f : (aown + a180 + a225 + a270)
								/ (float)(1 + Convert.ToInt32(!s180) + Convert.ToInt32(!s225) + Convert.ToInt32(!s270))
									/ (LiquidMap.MAX + 1);
							h2 = lqt090 || lqt135 || lqt180 ? 1.0f : (aown + a090 + a135 + a180)
								/ (float)(1 + Convert.ToInt32(!s090) + Convert.ToInt32(!s135) + Convert.ToInt32(!s180))
									/ (LiquidMap.MAX + 1);
							h3 = lqt000 || lqt045 || lqt090 ? 1.0f : (aown + a000 + a045 + a090)
								/ (float)(1 + Convert.ToInt32(!s000) + Convert.ToInt32(!s045) + Convert.ToInt32(!s090))
									/ (LiquidMap.MAX + 1);
							h4 = lqt000 || lqt270 || lqt315 ? 1.0f : (aown + a000 + a270 + a315)
								/ (float)(1 + Convert.ToInt32(!s000) + Convert.ToInt32(!s270) + Convert.ToInt32(!s315))
									/ (LiquidMap.MAX + 1);

							byte la000;
							byte la045;
							byte la090;
							byte la135;
							byte la180;
							byte la225;
							byte la270;
							byte la315;

							if (aown == LiquidMap.MAX) {
								la000 = s000 ? LiquidMap.MAX : a000;
								la045 = s045 ? LiquidMap.MAX : a045;
								la090 = s090 ? LiquidMap.MAX : a090;
								la135 = s135 ? LiquidMap.MAX : a135;
								la180 = s180 ? LiquidMap.MAX : a180;
								la225 = s225 ? LiquidMap.MAX : a225;
								la270 = s270 ? LiquidMap.MAX : a270;
								la315 = s315 ? LiquidMap.MAX : a315;
							} else {
								la000 = a000;
								la045 = a045;
								la090 = a090;
								la135 = a135;
								la180 = a180;
								la225 = a225;
								la270 = a270;
								la315 = a315;
							}

							if (la000 == la180 && la090 == la270 && la045 == la225 && la135 == la315) {
								dir = 0;
							} else if (la000 != la180 && la090 == la270) {
								if (la000 < la180 || la000 < aown || aown < la180)
									dir = 3;
								else
									dir = 7;
							} else if (la090 != la270 && la000 == la180) {
								if (la090 < la270 || la090 < aown || aown < la270)
									dir = 1;
								else
									dir = 5;
							} else if (la000 == la090 || la180 == la270) {
								if (la045 < la225 || la045 < aown || aown < la225)
									dir = 2;
								else
									dir = 6;
							} else if (la090 == la180 || la000 == la270) {
								if (la315 < la135 || la315 < aown || aown < la135)
									dir = 4;
								else
									dir = 8;
							}
						}
					}

					var uv1 = new Vector2(atlasPosition.x + 0f * atlasStep, atlasPosition.y + 0.0f * atlasStep);
					var uv2 = new Vector2(atlasPosition.x + 0f * atlasStep, atlasPosition.y + 1.0f * atlasStep);
					var uv3 = new Vector2(atlasPosition.x + 1f * atlasStep, atlasPosition.y + 1.0f * atlasStep);
					var uv4 = new Vector2(atlasPosition.x + 1f * atlasStep, atlasPosition.y + 0.0f * atlasStep);

					float aof1 = lr1 + lg1 + lb1 + ls1;
					float aof2 = lr2 + lg2 + lb2 + ls2;
					float aof3 = lr3 + lg3 + lb3 + ls3;
					float aof4 = lr4 + lg4 + lb4 + ls4;

					bool fliped = isLiquid && (lqt045 || lqt225);
					lock (lockObject) {
						if (isTwoSided) {
							AddTransparentFaceIndices(aof1, aof2, aof3, aof4, fliped);
							TransparentVertices.Add(Pack(x + 0, y + 1 * h1, z + 0, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
							TransparentVertices.Add(Pack(x + 0, y + 1 * h2, z + 1, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
							TransparentVertices.Add(Pack(x + 1, y + 1 * h3, z + 1, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
							TransparentVertices.Add(Pack(x + 1, y + 1 * h4, z + 0, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));
						} else {
							AddOpaqueFaceIndices(aof1, aof2, aof3, aof4, fliped);
							OpaqueVertices.Add(Pack(x + 0, y + 1 * h1, z + 0, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
							OpaqueVertices.Add(Pack(x + 0, y + 1 * h2, z + 1, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
							OpaqueVertices.Add(Pack(x + 1, y + 1 * h3, z + 1, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
							OpaqueVertices.Add(Pack(x + 1, y + 1 * h4, z + 0, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));
						}
					}
				}

				// Bottom face.
				if (HasFace(x, y - 1, z, isLiquid, blockType, localBlockCoordinate)) {
					var atlasPosition = (Vector2)blockData.TexturingData.BottomFace * atlasStep;

					bool t000 = !IsVoxelTransparent(x - 1, y - 1, z + 0);
					bool t090 = !IsVoxelTransparent(x + 0, y - 1, z + 1);
					bool t180 = !IsVoxelTransparent(x + 1, y - 1, z + 0);
					bool t270 = !IsVoxelTransparent(x + 0, y - 1, z - 1);

					int lrtop = GetLight(x + 0, y - 1, z + 0, LightMap.RED);
					int lr000 = GetLight(x - 1, y - 1, z + 0, LightMap.RED);
					int lr045 = GetLight(x - 1, y - 1, z + 1, LightMap.RED);
					int lr090 = GetLight(x + 0, y - 1, z + 1, LightMap.RED);
					int lr135 = GetLight(x + 1, y - 1, z + 1, LightMap.RED);
					int lr180 = GetLight(x + 1, y - 1, z + 0, LightMap.RED);
					int lr225 = GetLight(x + 1, y - 1, z - 1, LightMap.RED);
					int lr270 = GetLight(x + 0, y - 1, z - 1, LightMap.RED);
					int lr315 = GetLight(x - 1, y - 1, z - 1, LightMap.RED);

					float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 16.0f;
					float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 16.0f;
					float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 16.0f;
					float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 16.0f;

					int lgtop = GetLight(x + 0, y - 1, z + 0, LightMap.GREEN);
					int lg000 = GetLight(x - 1, y - 1, z + 0, LightMap.GREEN);
					int lg045 = GetLight(x - 1, y - 1, z + 1, LightMap.GREEN);
					int lg090 = GetLight(x + 0, y - 1, z + 1, LightMap.GREEN);
					int lg135 = GetLight(x + 1, y - 1, z + 1, LightMap.GREEN);
					int lg180 = GetLight(x + 1, y - 1, z + 0, LightMap.GREEN);
					int lg225 = GetLight(x + 1, y - 1, z - 1, LightMap.GREEN);
					int lg270 = GetLight(x + 0, y - 1, z - 1, LightMap.GREEN);
					int lg315 = GetLight(x - 1, y - 1, z - 1, LightMap.GREEN);

					float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 16.0f;
					float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 16.0f;
					float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 16.0f;
					float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 16.0f;

					int lbtop = GetLight(x + 0, y - 1, z + 0, LightMap.BLUE);
					int lb000 = GetLight(x - 1, y - 1, z + 0, LightMap.BLUE);
					int lb045 = GetLight(x - 1, y - 1, z + 1, LightMap.BLUE);
					int lb090 = GetLight(x + 0, y - 1, z + 1, LightMap.BLUE);
					int lb135 = GetLight(x + 1, y - 1, z + 1, LightMap.BLUE);
					int lb180 = GetLight(x + 1, y - 1, z + 0, LightMap.BLUE);
					int lb225 = GetLight(x + 1, y - 1, z - 1, LightMap.BLUE);
					int lb270 = GetLight(x + 0, y - 1, z - 1, LightMap.BLUE);
					int lb315 = GetLight(x - 1, y - 1, z - 1, LightMap.BLUE);

					float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 16.0f;
					float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 16.0f;
					float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 16.0f;
					float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 16.0f;

					int lstop = GetLight(x + 0, y - 1, z + 0, LightMap.SUN);
					int ls000 = GetLight(x - 1, y - 1, z + 0, LightMap.SUN);
					int ls045 = GetLight(x - 1, y - 1, z + 1, LightMap.SUN);
					int ls090 = GetLight(x + 0, y - 1, z + 1, LightMap.SUN);
					int ls135 = GetLight(x + 1, y - 1, z + 1, LightMap.SUN);
					int ls180 = GetLight(x + 1, y - 1, z + 0, LightMap.SUN);
					int ls225 = GetLight(x + 1, y - 1, z - 1, LightMap.SUN);
					int ls270 = GetLight(x + 0, y - 1, z - 1, LightMap.SUN);
					int ls315 = GetLight(x - 1, y - 1, z - 1, LightMap.SUN);

					float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 16.0f;
					float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 16.0f;
					float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 16.0f;
					float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 16.0f;

					float aof1 = lr1 + lg1 + lb1 + ls1;
					float aof2 = lr2 + lg2 + lb2 + ls2;
					float aof3 = lr3 + lg3 + lb3 + ls3;
					float aof4 = lr4 + lg4 + lb4 + ls4;

					int dir1 = 0;
					int dir2 = 0;
					int dir3 = 0;
					int dir4 = 0;

					var uv1 = new Vector2(atlasPosition.x + 0.0f * atlasStep, atlasPosition.y + 0.0f * atlasStep);
					var uv2 = new Vector2(atlasPosition.x + 0.0f * atlasStep, atlasPosition.y + 1.0f * atlasStep);
					var uv3 = new Vector2(atlasPosition.x + 1.0f * atlasStep, atlasPosition.y + 1.0f * atlasStep);
					var uv4 = new Vector2(atlasPosition.x + 1.0f * atlasStep, atlasPosition.y + 0.0f * atlasStep);

					lock (lockObject) {
						if (isTwoSided) {
							AddTransparentFaceIndices(aof1, aof2, aof3, aof4);
							TransparentVertices.Add(Pack(x + 1, y + 0, z + 0, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir1));
							TransparentVertices.Add(Pack(x + 1, y + 0, z + 1, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir2));
							TransparentVertices.Add(Pack(x + 0, y + 0, z + 1, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir3));
							TransparentVertices.Add(Pack(x + 0, y + 0, z + 0, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir4));
						} else {
							AddOpaqueFaceIndices(aof1, aof2, aof3, aof4);
							OpaqueVertices.Add(Pack(x + 1, y + 0, z + 0, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir1));
							OpaqueVertices.Add(Pack(x + 1, y + 0, z + 1, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir2));
							OpaqueVertices.Add(Pack(x + 0, y + 0, z + 1, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir3));
							OpaqueVertices.Add(Pack(x + 0, y + 0, z + 0, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir4));
						}
					}
				}

				// Front face.
				if (HasFace(x, y, z + 1, isLiquid, blockType, localBlockCoordinate)) {
					var atlasPosition = (Vector2)blockData.TexturingData.BackFace * atlasStep;

					bool t000 = !IsVoxelTransparent(x - 1, y + 0, z + 1);
					bool t090 = !IsVoxelTransparent(x + 0, y + 1, z + 1);
					bool t180 = !IsVoxelTransparent(x + 1, y + 0, z + 1);
					bool t270 = !IsVoxelTransparent(x + 0, y - 1, z + 1);

					int lrtop = GetLight(x + 0, y + 0, z + 1, LightMap.RED);
					int lr000 = GetLight(x - 1, y + 0, z + 1, LightMap.RED);
					int lr045 = GetLight(x - 1, y + 1, z + 1, LightMap.RED);
					int lr090 = GetLight(x + 0, y + 1, z + 1, LightMap.RED);
					int lr135 = GetLight(x + 1, y + 1, z + 1, LightMap.RED);
					int lr180 = GetLight(x + 1, y + 0, z + 1, LightMap.RED);
					int lr225 = GetLight(x + 1, y - 1, z + 1, LightMap.RED);
					int lr270 = GetLight(x + 0, y - 1, z + 1, LightMap.RED);
					int lr315 = GetLight(x - 1, y - 1, z + 1, LightMap.RED);

					float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 16.0f;
					float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 16.0f;
					float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 16.0f;
					float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 16.0f;

					int lgtop = GetLight(x + 0, y + 0, z + 1, LightMap.GREEN);
					int lg000 = GetLight(x - 1, y + 0, z + 1, LightMap.GREEN);
					int lg045 = GetLight(x - 1, y + 1, z + 1, LightMap.GREEN);
					int lg090 = GetLight(x + 0, y + 1, z + 1, LightMap.GREEN);
					int lg135 = GetLight(x + 1, y + 1, z + 1, LightMap.GREEN);
					int lg180 = GetLight(x + 1, y + 0, z + 1, LightMap.GREEN);
					int lg225 = GetLight(x + 1, y - 1, z + 1, LightMap.GREEN);
					int lg270 = GetLight(x + 0, y - 1, z + 1, LightMap.GREEN);
					int lg315 = GetLight(x - 1, y - 1, z + 1, LightMap.GREEN);

					float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 16.0f;
					float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 16.0f;
					float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 16.0f;
					float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 16.0f;

					int lbtop = GetLight(x + 0, y + 0, z + 1, LightMap.BLUE);
					int lb000 = GetLight(x - 1, y + 0, z + 1, LightMap.BLUE);
					int lb045 = GetLight(x - 1, y + 1, z + 1, LightMap.BLUE);
					int lb090 = GetLight(x + 0, y + 1, z + 1, LightMap.BLUE);
					int lb135 = GetLight(x + 1, y + 1, z + 1, LightMap.BLUE);
					int lb180 = GetLight(x + 1, y + 0, z + 1, LightMap.BLUE);
					int lb225 = GetLight(x + 1, y - 1, z + 1, LightMap.BLUE);
					int lb270 = GetLight(x + 0, y - 1, z + 1, LightMap.BLUE);
					int lb315 = GetLight(x - 1, y - 1, z + 1, LightMap.BLUE);

					float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 16.0f;
					float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 16.0f;
					float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 16.0f;
					float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 16.0f;

					int lstop = GetLight(x + 0, y + 0, z + 1, LightMap.SUN);
					int ls000 = GetLight(x - 1, y + 0, z + 1, LightMap.SUN);
					int ls045 = GetLight(x - 1, y + 1, z + 1, LightMap.SUN);
					int ls090 = GetLight(x + 0, y + 1, z + 1, LightMap.SUN);
					int ls135 = GetLight(x + 1, y + 1, z + 1, LightMap.SUN);
					int ls180 = GetLight(x + 1, y + 0, z + 1, LightMap.SUN);
					int ls225 = GetLight(x + 1, y - 1, z + 1, LightMap.SUN);
					int ls270 = GetLight(x + 0, y - 1, z + 1, LightMap.SUN);
					int ls315 = GetLight(x - 1, y - 1, z + 1, LightMap.SUN);

					float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 16.0f;
					float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 16.0f;
					float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 16.0f;
					float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 16.0f;

					float h1 = 0.0f;
					float h2 = 1.0f;
					float h3 = 1.0f;
					float h4 = 0.0f;

					int dir = 0;

					if (isLiquid) {
						if (atop == 0) {
							h2 = lqt000 || lqt045 || lqt090 ? 1.0f : (aown + a000 + a045 + a090)
								/ (float)(1 + Convert.ToInt32(!s000) + Convert.ToInt32(!s045) + Convert.ToInt32(!s090))
									/ (LiquidMap.MAX + 1);
							h3 = lqt090 || lqt135 || lqt180 ? 1.0f : (aown + a090 + a135 + a180)
								/ (float)(1 + Convert.ToInt32(!s090) + Convert.ToInt32(!s135) + Convert.ToInt32(!s180))
									/ (LiquidMap.MAX + 1);
						}
						if (a090 != 0) {
							h1 = (aown + a000 + a045 + a090)
								/ (float)(1 + Convert.ToInt32(!s000) + Convert.ToInt32(!s045) + Convert.ToInt32(!s090))
									/ (LiquidMap.MAX + 1);
							h4 = (aown + a090 + a135 + a180)
								/ (float)(1 + Convert.ToInt32(!s090) + Convert.ToInt32(!s135) + Convert.ToInt32(!s180))
									/ (LiquidMap.MAX + 1);
						}

						dir = 5;
					}

					var uv1 = new Vector2(atlasPosition.x + 0.0f * atlasStep, atlasPosition.y + h1 * atlasStep);
					var uv2 = new Vector2(atlasPosition.x + 0.0f * atlasStep, atlasPosition.y + h2 * atlasStep);
					var uv3 = new Vector2(atlasPosition.x + 1.0f * atlasStep, atlasPosition.y + h3 * atlasStep);
					var uv4 = new Vector2(atlasPosition.x + 1.0f * atlasStep, atlasPosition.y + h4 * atlasStep);

					float aof1 = lr1 + lg1 + lb1 + ls1;
					float aof2 = lr2 + lg2 + lb2 + ls2;
					float aof3 = lr3 + lg3 + lb3 + ls3;
					float aof4 = lr4 + lg4 + lb4 + ls4;

					lock (lockObject) {
						if (isTwoSided) {
							AddTransparentFaceIndices(aof1, aof2, aof3, aof4);
							TransparentVertices.Add(Pack(x + 1, y + h1, z + 1, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
							TransparentVertices.Add(Pack(x + 1, y + h2, z + 1, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
							TransparentVertices.Add(Pack(x + 0, y + h3, z + 1, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
							TransparentVertices.Add(Pack(x + 0, y + h4, z + 1, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));
						} else {
							AddOpaqueFaceIndices(aof1, aof2, aof3, aof4);
							OpaqueVertices.Add(Pack(x + 1, y + h1, z + 1, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
							OpaqueVertices.Add(Pack(x + 1, y + h2, z + 1, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
							OpaqueVertices.Add(Pack(x + 0, y + h3, z + 1, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
							OpaqueVertices.Add(Pack(x + 0, y + h4, z + 1, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));
						}
					}
				}

				// Back face.
				if (HasFace(x, y, z - 1, isLiquid, blockType, localBlockCoordinate)) {
					var atlasPosition = (Vector2)blockData.TexturingData.FrontFace * atlasStep;

					bool t000 = !IsVoxelTransparent(x + 1, y + 0, z - 1);
					bool t090 = !IsVoxelTransparent(x + 0, y + 1, z - 1);
					bool t180 = !IsVoxelTransparent(x - 1, y + 0, z - 1);
					bool t270 = !IsVoxelTransparent(x + 0, y - 1, z - 1);

					int lrtop = GetLight(x + 0, y + 0, z - 1, LightMap.RED);
					int lr000 = GetLight(x + 1, y + 0, z - 1, LightMap.RED);
					int lr045 = GetLight(x + 1, y + 1, z - 1, LightMap.RED);
					int lr090 = GetLight(x + 0, y + 1, z - 1, LightMap.RED);
					int lr135 = GetLight(x - 1, y + 1, z - 1, LightMap.RED);
					int lr180 = GetLight(x - 1, y + 0, z - 1, LightMap.RED);
					int lr225 = GetLight(x - 1, y - 1, z - 1, LightMap.RED);
					int lr270 = GetLight(x + 0, y - 1, z - 1, LightMap.RED);
					int lr315 = GetLight(x + 1, y - 1, z - 1, LightMap.RED);

					float lr1 = (t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270) / 4.0f / 16.0f;
					float lr2 = (t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180) / 4.0f / 16.0f;
					float lr3 = (t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090) / 4.0f / 16.0f;
					float lr4 = (t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315) / 4.0f / 16.0f;

					int lgtop = GetLight(x + 0, y + 0, z - 1, LightMap.GREEN);
					int lg000 = GetLight(x + 1, y + 0, z - 1, LightMap.GREEN);
					int lg045 = GetLight(x + 1, y + 1, z - 1, LightMap.GREEN);
					int lg090 = GetLight(x + 0, y + 1, z - 1, LightMap.GREEN);
					int lg135 = GetLight(x - 1, y + 1, z - 1, LightMap.GREEN);
					int lg180 = GetLight(x - 1, y + 0, z - 1, LightMap.GREEN);
					int lg225 = GetLight(x - 1, y - 1, z - 1, LightMap.GREEN);
					int lg270 = GetLight(x + 0, y - 1, z - 1, LightMap.GREEN);
					int lg315 = GetLight(x + 1, y - 1, z - 1, LightMap.GREEN);

					float lg1 = (t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270) / 4.0f / 16.0f;
					float lg2 = (t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180) / 4.0f / 16.0f;
					float lg3 = (t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090) / 4.0f / 16.0f;
					float lg4 = (t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315) / 4.0f / 16.0f;

					int lbtop = GetLight(x + 0, y + 0, z - 1, LightMap.BLUE);
					int lb000 = GetLight(x + 1, y + 0, z - 1, LightMap.BLUE);
					int lb045 = GetLight(x + 1, y + 1, z - 1, LightMap.BLUE);
					int lb090 = GetLight(x + 0, y + 1, z - 1, LightMap.BLUE);
					int lb135 = GetLight(x - 1, y + 1, z - 1, LightMap.BLUE);
					int lb180 = GetLight(x - 1, y + 0, z - 1, LightMap.BLUE);
					int lb225 = GetLight(x - 1, y - 1, z - 1, LightMap.BLUE);
					int lb270 = GetLight(x + 0, y - 1, z - 1, LightMap.BLUE);
					int lb315 = GetLight(x + 1, y - 1, z - 1, LightMap.BLUE);

					float lb1 = (t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270) / 4.0f / 16.0f;
					float lb2 = (t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180) / 4.0f / 16.0f;
					float lb3 = (t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090) / 4.0f / 16.0f;
					float lb4 = (t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315) / 4.0f / 16.0f;

					int lstop = GetLight(x + 0, y + 0, z - 1, LightMap.SUN);
					int ls000 = GetLight(x + 1, y + 0, z - 1, LightMap.SUN);
					int ls045 = GetLight(x + 1, y + 1, z - 1, LightMap.SUN);
					int ls090 = GetLight(x + 0, y + 1, z - 1, LightMap.SUN);
					int ls135 = GetLight(x - 1, y + 1, z - 1, LightMap.SUN);
					int ls180 = GetLight(x - 1, y + 0, z - 1, LightMap.SUN);
					int ls225 = GetLight(x - 1, y - 1, z - 1, LightMap.SUN);
					int ls270 = GetLight(x + 0, y - 1, z - 1, LightMap.SUN);
					int ls315 = GetLight(x + 1, y - 1, z - 1, LightMap.SUN);

					float ls1 = (t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270) / 4.0f / 16.0f;
					float ls2 = (t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180) / 4.0f / 16.0f;
					float ls3 = (t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090) / 4.0f / 16.0f;
					float ls4 = (t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315) / 4.0f / 16.0f;

					float h1 = 0.0f;
					float h2 = 1.0f;
					float h3 = 1.0f;
					float h4 = 0.0f;

					int dir = 0;

					if (isLiquid) {
						if (atop == 0) {
							h2 = lqt180 || lqt225 || lqt270 ? 1.0f : (aown + a180 + a225 + a270)
								/ (float)(1 + Convert.ToInt32(!s180) + Convert.ToInt32(!s225) + Convert.ToInt32(!s270))
									/ (LiquidMap.MAX + 1);
							h3 = lqt000 || lqt270 || lqt315 ? 1.0f : (aown + a000 + a270 + a315)
								/ (float)(1 + Convert.ToInt32(!s000) + Convert.ToInt32(!s270) + Convert.ToInt32(!s315))
									/ (LiquidMap.MAX + 1);
						}
						if (a270 != 0) {
							h1 = (aown + a180 + a225 + a270)
								/ (float)(1 + Convert.ToInt32(!s180) + Convert.ToInt32(!s225) + Convert.ToInt32(!s270))
									/ (LiquidMap.MAX + 1);
							h4 = (aown + a000 + a270 + a315)
								/ (float)(1 + Convert.ToInt32(!s000) + Convert.ToInt32(!s270) + Convert.ToInt32(!s315))
									/ (LiquidMap.MAX + 1);
						}

						dir = 5;
					}

					var uv1 = new Vector2(atlasPosition.x + 0.0f * atlasStep, atlasPosition.y + h1 * atlasStep);
					var uv2 = new Vector2(atlasPosition.x + 0.0f * atlasStep, atlasPosition.y + h2 * atlasStep);
					var uv3 = new Vector2(atlasPosition.x + 1.0f * atlasStep, atlasPosition.y + h3 * atlasStep);
					var uv4 = new Vector2(atlasPosition.x + 1.0f * atlasStep, atlasPosition.y + h4 * atlasStep);

					float aof1 = lr1 + lg1 + lb1 + ls1;
					float aof2 = lr2 + lg2 + lb2 + ls2;
					float aof3 = lr3 + lg3 + lb3 + ls3;
					float aof4 = lr4 + lg4 + lb4 + ls4;

					lock (lockObject) {
						if (isTwoSided) {
							AddTransparentFaceIndices(aof1, aof2, aof3, aof4);
							TransparentVertices.Add(Pack(x + 0, y + h1, z + 0, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
							TransparentVertices.Add(Pack(x + 0, y + h2, z + 0, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
							TransparentVertices.Add(Pack(x + 1, y + h3, z + 0, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
							TransparentVertices.Add(Pack(x + 1, y + h4, z + 0, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));
						} else {
							AddOpaqueFaceIndices(aof1, aof2, aof3, aof4);
							OpaqueVertices.Add(Pack(x + 0, y + h1, z + 0, uv1.x, uv1.y, lr1, lg1, lb1, ls1, dir));
							OpaqueVertices.Add(Pack(x + 0, y + h2, z + 0, uv2.x, uv2.y, lr2, lg2, lb2, ls2, dir));
							OpaqueVertices.Add(Pack(x + 1, y + h3, z + 0, uv3.x, uv3.y, lr3, lg3, lb3, ls3, dir));
							OpaqueVertices.Add(Pack(x + 1, y + h4, z + 0, uv4.x, uv4.y, lr4, lg4, lb4, ls4, dir));
						}
					}
				}
			}
		}
	}
}