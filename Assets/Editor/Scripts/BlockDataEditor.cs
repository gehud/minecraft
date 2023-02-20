using UnityEditor;
using UnityEngine;

namespace Minecraft.Editor {
	[CustomEditor(typeof(Block))]
	public class BlockDataEditor : UnityEditor.Editor {
		private SerializedProperty texturingData;
		private SerializedProperty materialType;
		private SerializedProperty isSolid;
		private SerializedProperty isLiquid;
		private SerializedProperty emission;
		private SerializedProperty isTransparent;
		private SerializedProperty absorption;

		private void OnEnable() {
			texturingData = serializedObject.FindProperty(nameof(texturingData));
			materialType = serializedObject.FindProperty(nameof(materialType));
			isSolid = serializedObject.FindProperty(nameof(isSolid));
			isLiquid = serializedObject.FindProperty(nameof(isLiquid));
			emission = serializedObject.FindProperty(nameof(emission));
			isTransparent = serializedObject.FindProperty(nameof(isTransparent));
			absorption = serializedObject.FindProperty(nameof(absorption));
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			EditorGUILayout.PropertyField(texturingData);
			EditorGUILayout.PropertyField(materialType);
			EditorGUILayout.PropertyField(isSolid);
			EditorGUILayout.PropertyField(isLiquid);
			EditorGUILayout.PropertyField(emission);
			EditorGUILayout.PropertyField(isTransparent);
			if (isTransparent.boolValue) {
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(absorption);
				EditorGUI.indentLevel--;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}