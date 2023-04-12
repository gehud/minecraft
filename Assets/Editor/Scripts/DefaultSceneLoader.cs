using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class DefaultSceneLoader {
	static DefaultSceneLoader() {
		if (EditorBuildSettings.scenes.Length == 0)
			return;
		var path = EditorBuildSettings.scenes[0].path;
		var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
		EditorSceneManager.playModeStartScene = sceneAsset;
	}
}