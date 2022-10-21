using UnityEngine;

namespace Minecraft
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance = null;
        public static T Instance => instance;

        protected void OnEnable()
        {
            if (instance)
                Destroy(this);

            instance = this as T;
        }

        protected void OnDisable()
        {
            instance = null;
        }
    }
}