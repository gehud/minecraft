using UnityEditor;

namespace Minecraft.Editor {
	[CustomEditor(typeof(Block))]
	public class BlockDataEditor : UnityEditor.Editor {
		private SerializedProperty texturingData;
		private SerializedProperty isSolid;
		private SerializedProperty isLiquid;
		private SerializedProperty isVegetation;
		private SerializedProperty emission;
		private SerializedProperty isTransparent;
		private SerializedProperty isTwoSided;
		private SerializedProperty absorption;

		private void OnEnable() {
			texturingData = serializedObject.FindProperty(nameof(texturingData));
			isSolid = serializedObject.FindProperty(nameof(isSolid));
			isLiquid = serializedObject.FindProperty(nameof(isLiquid));
			isVegetation = serializedObject.FindProperty(nameof(isVegetation));
			emission = serializedObject.FindProperty(nameof(emission));
			isTransparent = serializedObject.FindProperty(nameof(isTransparent));
			isTwoSided = serializedObject.FindProperty(nameof(isTwoSided));
			absorption = serializedObject.FindProperty(nameof(absorption));
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			EditorGUILayout.PropertyField(texturingData);
			EditorGUILayout.PropertyField(isSolid);
			EditorGUILayout.PropertyField(isLiquid);
			EditorGUILayout.PropertyField(isVegetation);
			EditorGUILayout.PropertyField(emission);
			EditorGUILayout.PropertyField(isTransparent);
			EditorGUILayout.PropertyField(isTwoSided);
			if (isTransparent.boolValue) {
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(absorption);
				EditorGUI.indentLevel--;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}