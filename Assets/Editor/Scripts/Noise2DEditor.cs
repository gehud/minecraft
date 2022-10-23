using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

namespace Minecraft.Noise
{
    [CustomEditor(typeof(Noise2D), true)]
    public class Noise2DEditor : Editor
    {
        struct PerlinNoise2DTextureJob : IJobFor, IDisposable
        {
            public NativeArray<Color> Colors;

            public static PerlinNoise2DTextureJob Create()
            {
                return new PerlinNoise2DTextureJob()
                {
                    Colors = new NativeArray<Color>(PREVIEW_SIZE * PREVIEW_SIZE, Allocator.TempJob),
                };
            }

            public void Execute(int index)
            {
                int x = index % PREVIEW_SIZE;
                int y = index / PREVIEW_SIZE;
                float value = noise.Sample(x, y);
                Colors[index] = new Color(value, value, value);
            }

            public JobHandle Schedule()
            {
                return this.Schedule(PREVIEW_SIZE * PREVIEW_SIZE, default);
            }

            public void Dispose()
            {
                Colors.Dispose();
            }
        }

        private const int PREVIEW_SIZE = 256;

        private static Noise2D noise;
        private Texture2D texture;

        private void OnEnable()
        {
            noise = (Noise2D)target;
            texture = new(PREVIEW_SIZE, PREVIEW_SIZE);
            UpdatePreviewTexture();
            Undo.undoRedoPerformed += UpdatePreviewTexture;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UpdatePreviewTexture;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
                UpdatePreviewTexture();
            GUILayout.Label("Preview");
            GUILayout.Box(texture);
        }

        private void UpdatePreviewTexture()
        {
            using PerlinNoise2DTextureJob job = PerlinNoise2DTextureJob.Create();

            job.Schedule().Complete();

            texture.SetPixels(job.Colors.ToArray());
            texture.Apply();
        }
    }
}