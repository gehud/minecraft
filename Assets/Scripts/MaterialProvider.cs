using UnityEngine;
using Zenject;

namespace Minecraft {
    public class MaterialProvider : MonoBehaviour {
        public Material[] Data => data;

        [SerializeField]
        private Material[] data;

        [Inject]
        private readonly AtlasProvider AtlasManager;

        public Material Get(MaterialType materialType) {
            return data[(int)materialType];
        }

        private void Awake() {
            for (int i = 0; i < data.Length; i++) {
                SetupAtlas((MaterialType)i, data[i], AtlasManager);
            }
        }

        private void SetupAtlas(MaterialType type, Material material, AtlasProvider atlasProvider) {
            material.SetTexture("_Atlas", atlasProvider.GetAtlas(type));
        }
    }
}