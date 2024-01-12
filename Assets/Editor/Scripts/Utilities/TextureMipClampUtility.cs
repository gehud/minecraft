using System.IO;
using UnityEditor;
using UnityEngine;

namespace Minecraft.Utilities.Editor {
    public class TextureMipClampUtility : EditorWindow {
        [SerializeField]
        private int maxMipLevel = 3;

        [MenuItem("Utilities/Texture Mip Clamp")]
        private static void Create() {
            GetWindow<TextureMipClampUtility>().Show();
        }

        private void OnGUI() {
            maxMipLevel = Mathf.Clamp(EditorGUILayout.IntField("Max Mip Level", maxMipLevel), 0, int.MaxValue);
            if (GUILayout.Button("Generate")) {
                GenerateTexture();
            }
        }

        private void GenerateTexture() {
            if (Selection.activeObject is not Texture2D texture) {
                Debug.Log("You must select a texture.");
                return;
            }

            var path = AssetDatabase.GetAssetPath(texture);
            var name = Path.GetFileName(path);
            name = name[..name.LastIndexOf('.')];
            path = path[..path.LastIndexOf('/')] + $"/{name}Copy.asset";

            var copy = new Texture2D(texture.width, texture.height, texture.format, maxMipLevel, false);
            copy.SetPixels32(texture.GetPixels32());
            copy.Apply();

            AssetDatabase.CreateAsset(copy, path);
            AssetDatabase.SaveAssets();
        }
    }
}