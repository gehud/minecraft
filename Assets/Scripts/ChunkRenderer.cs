using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft {
	public class ChunkRenderer {
		public Chunk Data { get; set; }

		private readonly Mesh sMesh;
		private readonly Mesh tMesh;

		private const MeshUpdateFlags UPDATE_FLAGS = 
			MeshUpdateFlags.DontRecalculateBounds | 
			MeshUpdateFlags.DontNotifyMeshUsers | 
			MeshUpdateFlags.DontResetBoneBounds | 
			MeshUpdateFlags.DontValidateIndices;

		public ChunkRenderer(Chunk data) {
			Data = data;
			sMesh = new();
			sMesh.MarkDynamic();
			tMesh = new();
			tMesh.MarkDynamic();
		}

		public void Render(Material sMaterial, Material tMaterial) {
			Matrix4x4 transform = Matrix4x4.Translate(Data.Coordinate * Chunk.SIZE);
			Graphics.DrawMesh(sMesh, transform, sMaterial, 0);
			Graphics.DrawMesh(tMesh, transform, tMaterial, 0);
		}

		public void UpdateMesh(ChunkRendererDataJob job) {
			var center = Vector3.one * Chunk.SIZE / 2.0f;
			var size = Vector3.one * Chunk.SIZE;
			var bounds = new Bounds(center, size);
			sMesh.SetVertexBufferParams(
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
			sMesh.SetVertexBufferData(job.OpaqueVertices, 0, 0, job.OpaqueVertices.Count, 0, UPDATE_FLAGS);
			sMesh.SetIndexBufferParams(job.OpaqueIndices.Count, IndexFormat.UInt16);
			sMesh.SetIndexBufferData(job.OpaqueIndices, 0, 0, job.OpaqueIndices.Count, UPDATE_FLAGS);
			sMesh.SetSubMesh(0, new SubMeshDescriptor(0, job.OpaqueIndices.Count), UPDATE_FLAGS);
			sMesh.bounds = bounds;
			tMesh.SetVertexBufferParams(
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
			tMesh.SetVertexBufferData(job.TransparentVertices, 0, 0, job.TransparentVertices.Count, 0, UPDATE_FLAGS);
			tMesh.SetIndexBufferParams(job.TransparentIndices.Count, IndexFormat.UInt16);
			tMesh.SetIndexBufferData(job.TransparentIndices, 0, 0, job.TransparentIndices.Count, UPDATE_FLAGS);
			tMesh.SetSubMesh(0, new SubMeshDescriptor(0, job.TransparentIndices.Count), UPDATE_FLAGS);
			tMesh.bounds = bounds;

			Data.IsDirty = false;
		}
	}
}