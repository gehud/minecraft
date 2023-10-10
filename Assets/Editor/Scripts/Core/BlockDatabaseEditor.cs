using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Minecraft.Editor {
    [CustomEditor(typeof(BlockDatabase))]
    public class BlockDatabaseEditor : UnityEditor.Editor {
        private ReorderableList list;

        private const float ELEMENT_DISTANCE = 2.0f;
        
        private void OnEnable() {
            list = new ReorderableList(serializedObject, serializedObject.FindProperty("pairs")) {
                drawHeaderCallback = DrawHeader,
                drawElementCallback = DrawElement,
                onAddDropdownCallback = OnAddDropdown
            };
        }

        private void DrawHeader(Rect rect) {
            GUI.Label(rect, "Data");
        }

        private bool IsKeyUnique(BlockType key) {
            for (int i = 0; i < list.serializedProperty.arraySize; i++) {
                var element = list.serializedProperty.GetArrayElementAtIndex(i);
                var value = (BlockType)element.FindPropertyRelative("Key").enumValueIndex;
                if (value == key) {
                    return false;
                }
            }

            return true;
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused) {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            rect.width /= 2.0f;
            rect.width -= ELEMENT_DISTANCE / 2.0f;
            rect.height = EditorGUIUtility.singleLineHeight;

            var key = element.FindPropertyRelative("Key");
            var value = element.FindPropertyRelative("Value");

            var oldKey = (BlockType)key.enumValueIndex;
            var newKey = (BlockType)EditorGUI.EnumPopup(rect, (BlockType)key.enumValueIndex);
            if (newKey != oldKey && IsKeyUnique(newKey)) {
                key.enumValueIndex = (int)newKey;
            }

            rect.x += rect.width + ELEMENT_DISTANCE;

            value.objectReferenceValue = EditorGUI.ObjectField(rect, value.objectReferenceValue, typeof(Block), false);
        }

        private void ClickHandler(object target) {
            var data = (BlockType)target;
            if (!IsKeyUnique(data)) {
                Debug.LogError("Key already presented in database.");
                return;
            }

            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("Key").enumValueIndex = (int)data;
            
            serializedObject.ApplyModifiedProperties();
        }


        private void OnAddDropdown(Rect rect, ReorderableList list) {
            var menu = new GenericMenu();
            foreach (var name in Enum.GetNames(typeof(BlockType))) {
                var value = Enum.Parse(typeof(BlockType), name);
                menu.AddItem(new GUIContent(name), false, ClickHandler, value);
            }

            menu.ShowAsContext();
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}