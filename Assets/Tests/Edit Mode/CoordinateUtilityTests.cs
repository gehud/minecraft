using NUnit.Framework;
using Minecraft.Utilities;
using UnityEngine;

namespace Minecraft.Tests.EditMode {
	public class CoordinateUtilityTests {
		[Test]
		public void Vector3ZeroIsVector3IntZero() {
			var position = Vector3.zero;
			var blockCoordinate = CoordinateUtility.ToCoordinate(position);
			Assert.AreEqual(blockCoordinate, Vector3Int.zero);
		}

		[Test]
		public void Vector3OneIsVector3IntOne() {
			var position = Vector3.one;
			var blockCoordinate = CoordinateUtility.ToCoordinate(position);
			Assert.AreEqual(blockCoordinate, Vector3Int.one);
		}

		[Test]
		public void Vector3NegativeOneIsVector3IntNegativeOne() {
			var position = -Vector3.one;
			var blockCoordinate = CoordinateUtility.ToCoordinate(position);
			Assert.AreEqual(blockCoordinate, -Vector3Int.one);
		}

		[Test]
		public void Vector3HalfOneIsVector3IntZero() {
			var position = Vector3.one / 2;
			var blockCoordinate = CoordinateUtility.ToCoordinate(position);
			Assert.AreEqual(blockCoordinate, Vector3Int.zero);
		}

		[Test]
		public void Vector3NegaiveHalfOneIsVector3IntNegativeOne() {
			var position = -Vector3.one / 2;
			var blockCoordinate = CoordinateUtility.ToCoordinate(position);
			Assert.AreEqual(blockCoordinate, -Vector3Int.one);
		}
	}
}