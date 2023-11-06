using Minecraft.Extensions;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

namespace Minecraft.Editor {
    [CustomEditor(typeof(NoiseSettings))]
    public class NoiseSettingsEditor : UnityEditor.Editor {
        private Texture2D preview;

        private const int previewSize = 256;

        private void OnEnable() {
            UpdatePreview();
        }

        public override void OnInspectorGUI() {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck()) {
                UpdatePreview();
            }

            GUILayout.Label("Preview");
            GUILayout.Box(preview);
        }

        [BurstCompile]
        private struct ImageJob : IJobFor, IDisposable {
            [ReadOnly]
            public Noise Noise;
            [ReadOnly]
            public float Zoom;
            [WriteOnly]
            public NativeArray<Color32> Colors;

            public void Execute(int index) {
                int x = index % previewSize;
                int y = index / previewSize;
                float value = Noise.Sample2D(x * Zoom, y * Zoom);

                var min = Noise.Modification.Keyframes.Length == 0 ? 0.0f : float.PositiveInfinity;
                for (int i = 0; i < Noise.Modification.Keyframes.Length; i++) {
                    var keyframeValue = Noise.Modification.Keyframes[i].value;
                    if (keyframeValue < min) {
                        min = keyframeValue;
                    }
                }

                var max = Noise.Modification.Keyframes.Length == 0 ? 1.0f : float.NegativeInfinity;
                for (int i = 0; i < Noise.Modification.Keyframes.Length; i++) {
                    var keyframeValue = Noise.Modification.Keyframes[i].value;
                    if (keyframeValue > max) {
                        max = keyframeValue;
                    }
                }
                value = value.Remap(min, max, 0.0f, 1.0f);
                Colors[index] = new Color(value, value, value);
            }

            public void Dispose() {
                Colors.Dispose();
            }
        }

        private void UpdatePreview() {
            var settings = (NoiseSettings)target;

            var job = new ImageJob {
                Noise = new Noise(settings, Allocator.TempJob),
                Zoom = 1.0f,
                Colors = new NativeArray<Color32>(previewSize * previewSize, Allocator.TempJob)
            };

            job.ScheduleParallel(previewSize * previewSize, previewSize, default).Complete();

            preview = new Texture2D(previewSize, previewSize);
            preview.SetPixels32(job.Colors.ToArray());
            preview.Apply();

            job.Noise.Dispose();
            job.Colors.Dispose();
        }
    }
}