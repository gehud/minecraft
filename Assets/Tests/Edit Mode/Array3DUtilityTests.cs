using NUnit.Framework;
using UnityEngine;

namespace Minecraft.Tests.EditMode {
	public class Array3DUtilityTests {
		private const int X_MAX = 3;
		private const int Y_MAX = 3;

		[Test]
		public void SamePlace() {
			int x = 2; 
			int y = 2; 
			int z = 2;
			int index = Array3DUtility.To1D(x, y, z, X_MAX, Y_MAX);
			Assert.AreEqual(Vector3Int.one * 2, Array3DUtility.To3D(index, X_MAX, Y_MAX));
		}
	}
}
