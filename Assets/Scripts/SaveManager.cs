using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

	public class SaveManager : MonoBehaviour {
		public event Action<SaveLoadData> OnLoad;

		[Inject]
		private readonly SceneLoader sceneLoader;

		private const string SAVE_EXCTENSION = ".world";
		private const string SAVE_TO_LOAD_KEY = "SaveToLoad";

		private string selectedPath;
		private readonly Dictionary<Vector3Int, long> savedPositions = new();

		public bool IsSaved(Vector3Int chunkCoordinate) {
			return savedPositions.ContainsKey(chunkCoordinate);
		}

		public void LoadSave(string saveName) {
			PlayerPrefs.SetString(SAVE_TO_LOAD_KEY, saveName);
			sceneLoader.LoadScene("Overworld");
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
			chunk.IsSaved = true;
			if (!IsSaved(chunk.Coordinate)) {
				using var binaryWriter = new BinaryWriter(File.Open(selectedPath, FileMode.Append));
				binaryWriter.Write(chunk.Coordinate.x);
				binaryWriter.Write(chunk.Coordinate.y);
				binaryWriter.Write(chunk.Coordinate.z);
				savedPositions.Add(chunk.Coordinate, binaryWriter.BaseStream.Position);
				binaryWriter.Write(chunk.BlockMap.Data.Cast<byte>().ToArray());
				binaryWriter.Write(chunk.LiquidMap.Data);
			} else {
				using var binaryWriter = new BinaryWriter(File.Open(selectedPath, FileMode.Open));
				binaryWriter.BaseStream.Position = savedPositions[chunk.Coordinate];
				binaryWriter.Write(chunk.BlockMap.Data.Cast<byte>().ToArray());
				binaryWriter.Write(chunk.LiquidMap.Data);
			}
		}

		public Chunk LoadChunk(World world, Vector3Int coordinate) {
			using var binaryReader = new BinaryReader(File.Open(selectedPath, FileMode.Open));
			binaryReader.BaseStream.Position += sizeof(float) * 2;
			while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length) {
				int x = binaryReader.ReadInt32();
				int y = binaryReader.ReadInt32();
				int z = binaryReader.ReadInt32();
				if (new Vector3Int(x, y, z) == coordinate) {
					var result = world.CreateChunk(coordinate);
					byte[] blockMap = new byte[Chunk.VOLUME];
					binaryReader.Read(blockMap, 0, Chunk.VOLUME);
					blockMap.CopyTo(result.BlockMap.Data, 0);
					binaryReader.Read(result.LiquidMap.Data, 0, Chunk.VOLUME);
					return result;
				}
				binaryReader.BaseStream.Position += Chunk.VOLUME * 2;
			}

			throw new Exception("Failed to load chunk.");
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

		private Vector2 SetupOffset() {
			using var binaryReader = new BinaryReader(File.Open(selectedPath, FileMode.Open));
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

		private void ExtractSavedChunkCoordinates() {
			using var binaryReader = new BinaryReader(File.Open(selectedPath, FileMode.Open));
			binaryReader.BaseStream.Position += sizeof(float) * 2;
			while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length) {
				int x = binaryReader.ReadInt32();
				int y = binaryReader.ReadInt32();
				int z = binaryReader.ReadInt32();
				savedPositions.Add(new Vector3Int(x, y, z), binaryReader.BaseStream.Position);
				binaryReader.BaseStream.Position += Chunk.VOLUME * 2;
			}
		}

		private void Start() {
			if (!PlayerPrefs.HasKey(SAVE_TO_LOAD_KEY))
				return;
			var saveName = PlayerPrefs.GetString(SAVE_TO_LOAD_KEY);
			selectedPath = GetSavePath(saveName);
			var offset = SetupOffset();
			ExtractSavedChunkCoordinates();
			OnLoad?.Invoke(new SaveLoadData(offset));
			PlayerPrefs.DeleteKey(SAVE_TO_LOAD_KEY);
		}
	}
}