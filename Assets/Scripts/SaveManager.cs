using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Minecraft {
	public struct SaveLoadData {
		public Vector2 Offset => offset;

		private Vector2 offset;

		public SaveLoadData(Vector2 offset) {
			this.offset = offset;
		}
	}

	public class SaveManager : NetworkBehaviour {
		public event Action<SaveLoadData> OnLoad;

		private const int PAYLOAD_SIZE = 4096;

		[Inject]
		private readonly ISceneManager sceneManager;

		[Inject]
		private readonly ISavePayload savePayload;

		private const string SAVE_EXCTENSION = ".world";

		private string selectedPath;

		private SaveLoadData? saveLoadData;
		private readonly Dictionary<Vector3Int, Chunk> savedChunks = new();

		public bool IsSaved(Vector3Int chunkCoordinate) {
			return savedChunks.ContainsKey(chunkCoordinate);
		}

		private void LoadGame() {
			sceneManager.LoadScene("Overworld");
		}

		public void HostGame(string saveName) {
			savePayload.Name = saveName;
			savePayload.Role = ConnectionRoles.Host;
			LoadGame();
		}

		public void LoadGame(string saveName) {
			savePayload.Name = saveName;
			savePayload.Role = ConnectionRoles.None;
			LoadGame();
		}

		public void ConnectToLocalHost() {
			savePayload.Name = "__SERVER__";
			savePayload.Role = ConnectionRoles.Client;
			LoadGame();
		}

		public string CreateSave(string saveName) {
			string[] files = Directory.GetFiles(Path.Combine(Application.persistentDataPath + "/saves/"));
			foreach (string file in files) {
				var fileName = Path.GetFileNameWithoutExtension(file);
				if (fileName == saveName) {
					if (int.TryParse(fileName[^1].ToString(), out int lastNumber)) {
						var fileNameBase = fileName[..^1];
						saveName = fileNameBase + (++lastNumber);
					} else {
						saveName = fileName + "_1";
					}
				}
			}

			File.Create(GetSavePath(saveName)).Close();
			return saveName;
		}

		public void DeleteSave(string saveName) {
			File.Delete(GetSavePath(saveName));
		}

		public void SaveChunk(Chunk chunk) {
			chunk.IsModified = false;
			chunk.IsSaved = true;
			if (!savedChunks.ContainsKey(chunk.Coordinate))
				savedChunks.Add(chunk.Coordinate, chunk);
			if (savePayload.Role == ConnectionRoles.None)
				return;
			byte[] buffer = new byte[3 * sizeof(int) + Chunk.VOLUME * sizeof(BlockType) + Chunk.VOLUME * sizeof(byte)];
			var stream = new MemoryStream(buffer);
			var binaryWriter = new BinaryWriter(stream);
			binaryWriter.Write(chunk.Coordinate.x);
			binaryWriter.Write(chunk.Coordinate.y);
			binaryWriter.Write(chunk.Coordinate.z);
			unsafe {
				fixed (BlockType* sourceData = chunk.BlockMap.Data)
					binaryWriter.Write(new ReadOnlySpan<byte>(sourceData, chunk.BlockMap.Data.Length * sizeof(BlockType)));
				fixed (byte* sourceData = chunk.LiquidMap.Data)
					binaryWriter.Write(new ReadOnlySpan<byte>(sourceData, chunk.LiquidMap.Data.Length * sizeof(byte)));
			}
			if (IsClient && !IsHost) {
				TrySaveChunkFromDataServerRpc(chunk.Coordinate, buffer);
			} else {
				TrySaveChunkFromDataClientRpc(chunk.Coordinate, buffer);
			}
		}

		private void TrySaveChunkFromData(Vector3Int coordinate, byte[] data) {
			var world = World.Instance;

			var binaryReader = new BinaryReader(new MemoryStream(data, false));
			int x = binaryReader.ReadInt32();
			int y = binaryReader.ReadInt32();
			int z = binaryReader.ReadInt32();
			Chunk chunk;
			if (world.HasChunk(coordinate)) {
				chunk = world.GetChunk(coordinate);
				chunk.IsDirty = true;
				chunk.IsSaved = true;
			} else {
				chunk = new Chunk {
					Coordinate = coordinate,
					IsSaved = true
				};
				world.SetChunk(coordinate, chunk);
			}
			var blockMapBytes = binaryReader.ReadBytes(Chunk.VOLUME * sizeof(BlockType));
			var liquidMapBytes = binaryReader.ReadBytes(Chunk.VOLUME * sizeof(byte));
			unsafe {
				fixed (byte* sourceData = blockMapBytes)
					fixed (BlockType* destinationData = chunk.BlockMap.Data)
						Buffer.MemoryCopy(sourceData, destinationData, sizeof(BlockType) * chunk.BlockMap.Data.Length, sizeof(BlockType) * chunk.BlockMap.Data.Length);
			}
			unsafe {
				fixed (byte* sourceData = liquidMapBytes)
					fixed (byte* destinationData = chunk.LiquidMap.Data)
						Buffer.MemoryCopy(sourceData, destinationData, sizeof(byte) * chunk.LiquidMap.Data.Length, sizeof(byte) * chunk.LiquidMap.Data.Length);
			}

			if (world.HasRenderer(chunk.Coordinate))
				world.GetRenderer(chunk.Coordinate).Chunk = chunk;

			if (!savedChunks.ContainsKey(coordinate)) {
				savedChunks.Add(coordinate, chunk);
			}
		}

		[ServerRpc(RequireOwnership = false)]
		private void TrySaveChunkFromDataServerRpc(Vector3Int coordinate, byte[] data) {
			TrySaveChunkFromData(coordinate, data);
		}

		[ClientRpc]
		private void TrySaveChunkFromDataClientRpc(Vector3Int coordinate, byte[] data) {
			TrySaveChunkFromData(coordinate, data);
		}

		public Chunk LoadChunk(World world, Vector3Int coordinate) {
			if (!IsSaved(coordinate))
				throw new Exception("Chunk is unsaved.");
			var chunk = savedChunks[coordinate];
			world.SetChunk(coordinate, chunk);
			return chunk;
		}

		public IEnumerable<string> GetAllSaveNames() {
			string dataPath = Path.Combine(Application.persistentDataPath + "/saves/");
			if (!Directory.Exists(dataPath))
				Directory.CreateDirectory(dataPath);
			string[] files = Directory.GetFiles(dataPath, "*.world");
			foreach (string file in files)
				yield return Path.GetFileNameWithoutExtension(file);
		}

		private string GetSavePath(string saveName) {
			return Path.Combine(Application.persistentDataPath + "/saves/") + saveName + SAVE_EXCTENSION;
		}

		private Vector2 SetupOffset(byte[] bytes) {
			using var binaryReader = new BinaryReader(new MemoryStream(bytes));
			float offsetX;
			float offsetY;
			if (binaryReader.BaseStream.Length == 0) {
				binaryReader.Close();
				using var binaryWriter = new BinaryWriter(File.Open(selectedPath, FileMode.Open));
				offsetX = UnityEngine.Random.Range(-3529.2f, 3529.2f);
				offsetY = UnityEngine.Random.Range(-3529.2f, 3529.2f);
				binaryWriter.Write(offsetX);
				binaryWriter.Write(offsetY);
			} else {
				offsetX = binaryReader.ReadSingle();
				offsetY = binaryReader.ReadSingle();
				binaryReader.Close();
			}

			return new Vector2(offsetX, offsetY);
		}

		private void ExtractSavedChunks(byte[] bytes) {
			var stream = new MemoryStream(bytes);
			using var binaryReader = new BinaryReader(stream);
			binaryReader.BaseStream.Position += sizeof(float) * 2;
			while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length) {
				int x = binaryReader.ReadInt32();
				int y = binaryReader.ReadInt32();
				int z = binaryReader.ReadInt32();
				var coordinate = new Vector3Int(x, y, z);
				var chunk = new Chunk {
					Coordinate = coordinate,
					IsSaved = true
				};
				var blockMapBytes = binaryReader.ReadBytes(Chunk.VOLUME * sizeof(BlockType));
				var liquidMapBytes = binaryReader.ReadBytes(Chunk.VOLUME * sizeof(byte));
				unsafe {
					fixed (byte* sourceData = blockMapBytes)
					fixed (BlockType* destinationData = chunk.BlockMap.Data)
						Buffer.MemoryCopy(sourceData, destinationData, sizeof(BlockType) * chunk.BlockMap.Data.Length, sizeof(BlockType) * chunk.BlockMap.Data.Length);
				}
				unsafe {
					fixed (byte* sourceData = liquidMapBytes)
					fixed (byte* destinationData = chunk.LiquidMap.Data)
						Buffer.MemoryCopy(sourceData, destinationData, sizeof(byte) * chunk.LiquidMap.Data.Length, sizeof(byte) * chunk.LiquidMap.Data.Length);
				}
				savedChunks.Add(coordinate, chunk);
			}
		}

		private void InitializeOnServer(string saveName) {
			selectedPath = GetSavePath(saveName);
			var data = File.ReadAllBytes(selectedPath);
			var offset = SetupOffset(data);
			saveLoadData = new SaveLoadData(offset);
			ExtractSavedChunks(data);
			OnLoad?.Invoke(saveLoadData.Value);
		}

		public override void OnNetworkSpawn() {
			var saveName = savePayload.Name;
			if (string.IsNullOrEmpty(saveName))
				return;
			if (IsServer) {
				InitializeOnServer(saveName);
			} else {
				QerryWorldDataServerRpc();
			}
		}

		private void Start() {
			if (savePayload.Role != ConnectionRoles.None)
				return;
			var saveName = savePayload.Name;
			if (string.IsNullOrEmpty(saveName))
				return;
			InitializeOnServer(saveName);
		}

		[ServerRpc(RequireOwnership = false)]
		private void QerryWorldDataServerRpc() {
			var data = File.ReadAllBytes(selectedPath);

			AllocateWorldDataBufferClientRpc(data.Length);
			PassSaveOffsetClientRpc(saveLoadData.Value.Offset);

			int packageCount = Mathf.CeilToInt(data.Length / PAYLOAD_SIZE);
			for (int x = 0; x < packageCount; x++) {
				byte[] package;
				if (x == packageCount - 1) {
					package = new byte[data.Length - (packageCount - 1) * PAYLOAD_SIZE];
				} else {
					package = new byte[PAYLOAD_SIZE];
				}

				Buffer.BlockCopy(data, x * PAYLOAD_SIZE, package, 0, package.Length);

				PassWorldDataPackageClientRpc(package, x * PAYLOAD_SIZE);
			}

			ExtractSavedChunksClientRpc();
		}

		private byte[] worldData;

		[ClientRpc]
		private void AllocateWorldDataBufferClientRpc(int size) {
			if (IsHost)
				return;
			worldData = new byte[size];
		}

		[ClientRpc]
		private void PassSaveOffsetClientRpc(Vector2 value) {
			if (IsHost)
				return;
			saveLoadData = new SaveLoadData(value);
		}

		[ClientRpc]
		private void PassWorldDataPackageClientRpc(byte[] data, int offset) {
			if (IsHost)
				return;
			Buffer.BlockCopy(data, 0, worldData, offset, data.Length);
		}

		[ClientRpc]
		private void ExtractSavedChunksClientRpc() {
			if (IsHost)
				return;
			ExtractSavedChunks(worldData);
			OnLoad?.Invoke(saveLoadData.Value);
			worldData = null;
		}

		private new void OnDestroy() {
			if (savePayload.Role != ConnectionRoles.None && IsClient || saveLoadData == null)
				return;

			savePayload.Role = ConnectionRoles.None;

			var binaryWriter = new BinaryWriter(File.OpenWrite(selectedPath));
			binaryWriter.Write(saveLoadData.Value.Offset.x);
			binaryWriter.Write(saveLoadData.Value.Offset.y);
			foreach (var pair in savedChunks) {
				binaryWriter.Write(pair.Key.x);
				binaryWriter.Write(pair.Key.y);
				binaryWriter.Write(pair.Key.z);
				unsafe {
					fixed (BlockType* sourceData = pair.Value.BlockMap.Data)
						binaryWriter.Write(new ReadOnlySpan<byte>(sourceData, pair.Value.BlockMap.Data.Length * sizeof(BlockType)));
					fixed (byte* sourceData = pair.Value.LiquidMap.Data)
						binaryWriter.Write(new ReadOnlySpan<byte>(sourceData, pair.Value.LiquidMap.Data.Length * sizeof(byte)));
				}
			}
		}
	}
}