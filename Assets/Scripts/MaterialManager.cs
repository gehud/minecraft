using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Minecraft {
    public class MaterialManager : MonoBehaviour {
        public IReadOnlyDictionary<MaterialType, Material> Materials => materials;
        private readonly Dictionary<MaterialType, Material> materials = new();

        [Serializable]
        private struct MaterialPair {
            public MaterialType Type;
            public Material Material;
        }

        [SerializeField]
        private List<MaterialPair> materialPairs = new();

        [Inject]
        private AtlasManager AtlasManager { get; }

        private void Awake() {
            foreach (var item in materialPairs) {
                materials.Add(item.Type, item.Material);
            }
        }

        private void Start() {
            foreach (var item in Materials) {
                SetupAtlas(item.Key, item.Value, AtlasManager);
            }
        }

        private void SetupAtlas(MaterialType type, Material material, AtlasManager atlasProvider) {
            material.SetTexture("_Atlas", atlasProvider.GetAtlas(type));
        }
    }
}