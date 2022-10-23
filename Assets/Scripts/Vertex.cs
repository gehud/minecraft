namespace Minecraft
{
    public struct Vertex
    {
        public float X, Y, Z, U, V, R, G, B, S;

        public Vertex(float x, float y, float z, float u, float v, float r, float g, float b, float s)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
            R = r;
            G = g;
            B = b;
            S = s;
        }
    }
}