namespace Minecraft {
    public struct Vertex {
		public float X, Z, U, V, RG, BS, D;

		public Vertex(float x, float z, float u, float v, float rG, float bS, float d) {
			X = x;
			Z = z;
			U = u;
			V = v;
			RG = rG;
			BS = bS;
			D = d;
		}
	}
}