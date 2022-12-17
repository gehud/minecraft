using NUnit.Framework;
using UnityEngine;

namespace Minecraft.Tests.EditMode {
	public class Array3DTests {
		[Test]
		public void SamePlaceWithIntegers() {
			var array = new Array3D<int>(3, 3, 3);
			array[2, 2, 2] = 1;
			Assert.AreEqual(1, array[2, 2, 2]);
		}

		[Test]
		public void SamePlaceWithVector3Int() {
			var array = new Array3D<int>(3, 3, 3);
			array[Vector3Int.one * 2] = 1;
			Assert.AreEqual(1, array[Vector3Int.one * 2]);
		}
	}
}
