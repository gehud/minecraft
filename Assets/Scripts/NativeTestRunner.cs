using System.Runtime.InteropServices;
using UnityEngine;

namespace Minecraft {
	public class NativeTestRunner : MonoBehaviour {
		[DllImport("Native")]
		static extern int NativeTest();

		private void Start() {
			Debug.Log(NativeTest());
		}
	}
}