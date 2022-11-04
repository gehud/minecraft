using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft {
    public class MaterialManager : Singleton<MaterialManager> {
        public IReadOnlyDictionary<MaterialType, Material> Materials => materials;
        private readonly Dictionary<MaterialType, Material> materials = new();

        [Serializable]
        private struct MaterialPair {
            public MaterialType Type;
            public Material Material;
        }

        [SerializeField]
        private List<MaterialPair> materialPairs = new();

        private void Awake() {
            foreach (var item in materialPairs) {
                materials.Add(item.Type, item.Material);
            }
        }

        private void Start() {
            var atlasManager = AtlasProvider.Instance;
            foreach (var item in Materials) {
                if (atlasManager != null)
                    SetupAtlas(item.Value, atlasManager);
            }
        }

        private void SetupAtlas(Material material, IAtlasProvider atlasProvider) {
            material.SetTexture("_Atlas", atlasProvider.Atlas);
        }
    }
}