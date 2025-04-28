using UnityEngine;

namespace Minecraft.UI
{
    public class LoadingPanel : MonoBehaviour
    {
        private void OnGameLoaded()
        {
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            GameLoadingSystem.GameLoaded += OnGameLoaded;
        }

        private void OnDisable()
        {
            GameLoadingSystem.GameLoaded -= OnGameLoaded;
        }
    }
}