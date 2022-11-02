using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft {
    public class MaterialManager : Singleton<MaterialManager> {
        public IReadOnlyDictionary<MaterialType, Material> Materials => materials;
        private Dictionary<MaterialType, Material> materials = new();

        [Serializable]
        private struct MaterialPair {
            public MaterialType Type;
            public Material Material;
        }

        [SerializeField]
        private List<MaterialPair> materialPairs = new();

        private void Awake() {
            foreach (var item in materialPairs)
                materials.Add(item.Type, item.Material);
        }
    }
}