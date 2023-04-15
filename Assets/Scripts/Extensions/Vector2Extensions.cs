namespace Minecraft.Extensions {
	public static class Vector2Extensions {
		public static UnityEngine.Vector2 NormalizeSmooth(this UnityEngine.Vector2 vector2) {
			return vector2.magnitude <= 1.0f ? vector2 : vector2.normalized;
		}
	}
}