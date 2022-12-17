using NUnit.Framework;

namespace Minecraft.Tests.EditMode {
	public class LiquidMapTests {
		[Test]
		public void SameAmount() {
			var liquidMap = new LiquidMap();
			liquidMap.Set(2, 2, 2, BlockType.Water, LiquidMap.MAX);
			Assert.AreEqual(LiquidMap.MAX, liquidMap.Get(2, 2, 2, BlockType.Water));
		}

		[Test]
		public void SameFromArrayIndexing() {
			var liquidMap = new LiquidMap();
			liquidMap[2, 2, 2] = new LiquidData(BlockType.Water, LiquidMap.MAX);
			Assert.AreEqual(LiquidMap.MAX, liquidMap.Get(2, 2, 2, BlockType.Water));
		}
	}
}
