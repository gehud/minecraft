using System.Collections.Generic;

namespace Minecraft {
	public class ChunkRendererData {
        public List<Vertex> OpaqueVertices = new();
        public List<ushort> OpaqueIndices = new();
		public List<Vertex> TransparentVertices = new();
		public List<ushort> TransparentIndices = new();
	}
}