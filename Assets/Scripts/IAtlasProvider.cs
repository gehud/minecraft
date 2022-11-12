using UnityEngine;

namespace Minecraft {
    public interface IAtlasProvider {
        float TileStep { get; }
        Texture2D GetAtlas(MaterialType materialType);
    }
}