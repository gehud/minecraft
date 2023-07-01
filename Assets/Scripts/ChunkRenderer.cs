using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft {
	public class ChunkRenderer {
		public Chunk Chunk { get; set; }

		private readonly Mesh opaqueMesh;
		private readonly Mesh transparentMesh;

		private const MeshUpdateFlags UPDATE_FLAGS =
			MeshUpdateFlags.DontRecalculateBounds |
			MeshUpdateFlags.DontNotifyMeshUsers |
			MeshUpdateFlags.DontResetBoneBounds
#if !DEBUG
			| MeshUpdateFlags.DontValidateIndices;
#else
			;
#endif

		public ChunkRenderer(Chunk chunk) {
			Chunk = chunk;
			opaqueMesh = new();
			transparentMesh = new();
		}

		public void Render(Material opaqueMaterial, Material transparentMaterial) {
			Matrix4x4 transform = Matrix4x4.Translate(Chunk.Coordinate * Chunk.SIZE);
			Graphics.DrawMesh(opaqueMesh, transform, opaqueMaterial, 0);
			Graphics.DrawMesh(transparentMesh, transform, transparentMaterial, 0);
		}

		public void Update(ChunkUpdateJob job) {
			var center = Vector3.one * Chunk.SIZE / 2.0f;
			var size = Vector3.one * Chunk.SIZE;
			var bounds = new Bounds(center, size);
			opaqueMesh.SetVertexBufferParams(
				job.OpaqueVertices.Count,
				new VertexAttributeDescriptor(
					VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2
				),
				new VertexAttributeDescriptor(
					VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 2
				),
				new VertexAttributeDescriptor(
					VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 2
				),
				new VertexAttributeDescriptor(
					VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 1
				)
			);
			opaqueMesh.SetVertexBufferData(job.OpaqueVertices, 0, 0, job.OpaqueVertices.Count, 0, UPDATE_FLAGS);
			opaqueMesh.SetIndexBufferParams(job.OpaqueIndices.Count, IndexFormat.UInt16);
			opaqueMesh.SetIndexBufferData(job.OpaqueIndices, 0, 0, job.OpaqueIndices.Count, UPDATE_FLAGS);
			opaqueMesh.SetSubMesh(0, new SubMeshDescriptor(0, job.OpaqueIndices.Count), UPDATE_FLAGS);
			opaqueMesh.bounds = bounds;

			transparentMesh.SetVertexBufferParams(
				job.TransparentVertices.Count,
				new VertexAttributeDescriptor(
					VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2
				),
				new VertexAttributeDescriptor(
					VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 2
				),
				new VertexAttributeDescriptor(
					VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 2
				),
				new VertexAttributeDescriptor(
					VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 1
				)
			);
			transparentMesh.SetVertexBufferData(job.TransparentVertices, 0, 0, job.TransparentVertices.Count, 0, UPDATE_FLAGS);
			transparentMesh.SetIndexBufferParams(job.TransparentIndices.Count, IndexFormat.UInt16);
			transparentMesh.SetIndexBufferData(job.TransparentIndices, 0, 0, job.TransparentIndices.Count, UPDATE_FLAGS);
			transparentMesh.SetSubMesh(0, new SubMeshDescriptor(0, job.TransparentIndices.Count), UPDATE_FLAGS);
			transparentMesh.bounds = bounds;

			Chunk.IsDirty = false;
		}

		public void Update(World world, Chunk chunk, BlockProvider blockProvider) {
			Update(new ChunkUpdateJob(world, chunk, blockProvider));
		}
	}
}