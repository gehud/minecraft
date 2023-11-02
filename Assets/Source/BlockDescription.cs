using System;
using Unity.Mathematics;
using UnityEngine;

namespace Minecraft {
    [Serializable]
    public struct Texturing {
        public int2 Right;
        public int2 Left;
        public int2 Top;
        public int2 Bottom;
        public int2 Front;
        public int2 Back;
    }

    [CreateAssetMenu]
    public class BlockDescription : ScriptableObject {
        public Texturing Texturing => texturing;

        public bool IsSolid => isSolid;

        public bool IsTransparent => isTransparent;

        public int Absorption => absorption;

        public LightColor Emission => emission;

        [SerializeField]
        private Texturing texturing;
        [SerializeField]
        private bool isSolid = true;
        [SerializeField]
        private bool isTransparent = false;
        [SerializeField, Range(0, 15)]
        private int absorption = 0;
        [SerializeField]
        private LightColor emission;
    }
}