namespace Minecraft.Lighting {
    public struct Light {
        public const byte Min = 0;
        public const byte Max = 15;

        private ushort representation;

        public readonly byte Get(LightChanel chanel) {
            return (byte)(representation >> ((byte)chanel << 2) & 0xF);
        }

        public void Set(LightChanel chanel, byte value) {
            representation = (ushort)(representation & (0xFFFF & ~(0xF << (byte)chanel * 4)) | value << ((byte)chanel << 2));
        }

        public byte Red {
            readonly get => (byte)(representation & 0xF);
            set => representation = (ushort)(representation & 0xFFF0 | value);
        }

        public byte Green {
            readonly get => (byte)(representation >> 4 & 0xF);
            set => representation = (ushort)(representation & 0xFF0F | value << 4);
        }

        public byte Blue {
            readonly get => (byte)(representation >> 8 & 0xF);
            set => representation = (ushort)(representation & 0xF0FF | value << 8);
        }

        public byte Sun {
            readonly get => (byte)(representation >> 12 & 0xF);
            set => representation = (ushort)(representation & 0x0FFF | value << 12);
        }

        public override readonly string ToString() {
            return $"Light: (R: {Red}, G: {Green}, B: {Blue}, S: {Sun})";
        }
    }
}