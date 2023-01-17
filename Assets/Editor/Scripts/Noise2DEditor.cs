using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using Minecraft.Extensions;

namespace Minecraft.Noise.Editor {
    [CustomEditor(typeof(Noise2D), true)]
    public class Noise2DEditor : UnityEditor.Editor {
        struct PerlinNoise2DTextureJob : IJobFor, IDisposable {
            public NativeArray<Color32> Colors;

            public static PerlinNoise2DTextureJob Create() {
                return new PerlinNoise2DTextureJob() {
                    Colors = new NativeArray<Color32>(PREVIEW_SIZE * PREVIEW_SIZE, Allocator.TempJob),
                };
            }

            public void Execute(int index) {
                int x = index % PREVIEW_SIZE;
                int y = index / PREVIEW_SIZE;
                float value = noise.Sample(x * zoom, y * zoom);
                value = value.Remap(min, max, 0.0f, 1.0f);
                Colors[index] = new Color(value, value, value);
            }

            public JobHandle Schedule() {
                return this.Schedule(PREVIEW_SIZE * PREVIEW_SIZE, default);
            }

            public void Dispose() {
                Colors.Dispose();
            }
        }

        private const int PREVIEW_SIZE = 256;

        private static Noise2D noise;
        private Texture2D texture;
        private static float zoom = 1.0f;
        private static float min = 0.0f;    
        private static float max = 0.0f;    

        private void OnEnable() {
            noise = (Noise2D)target;
            texture = new(PREVIEW_SIZE, PREVIEW_SIZE);
            min = noise.Min;
            max = noise.Max;
            UpdatePreviewTexture();
            Undo.undoRedoPerformed += UpdatePreviewTexture;
        }

        private void OnDisable() {
            Undo.undoRedoPerformed -= UpdatePreviewTexture;
        }

        public override void OnInspectorGUI() {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            GUILayout.Label("Preview");
            zoom = EditorGUILayout.FloatField("Zoom", zoom);
            if (EditorGUI.EndChangeCheck())
                UpdatePreviewTexture();
            GUILayout.Box(texture);
        }

        private void UpdatePreviewTexture() {
            using PerlinNoise2DTextureJob job = PerlinNoise2DTextureJob.Create();

            job.Schedule().Complete();

            texture.SetPixels32(job.Colors.ToArray());
            texture.Apply();

            min = noise.Min;
            max = noise.Max;
        }
    }
}