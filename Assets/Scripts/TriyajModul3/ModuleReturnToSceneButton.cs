using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TriyajModul3
{
    /// <summary>
    /// UI butonundan belirli bir sahneye guvenli donus yapar.
    /// </summary>
    [DisallowMultipleComponent]
    public class ModuleReturnToSceneButton : MonoBehaviour
    {
        private const string DefaultSceneName = "Modul1";

        [Header("Sahne Ayari")]
        [Tooltip("Tercih edilen hedef sahne adi. Bos veya gecersiz ise build settings icinden eslesen sahne bulunur.")]
        [SerializeField] private string targetSceneName = DefaultSceneName;

        [Header("UI")]
        [Tooltip("Tiklandiginda devre disi birakilacak buton. Bos ise ayni objeden Button aranir.")]
        [SerializeField] private Button sourceButton;

        private bool isLoading;

        private void Awake()
        {
            if (sourceButton == null)
            {
                sourceButton = GetComponent<Button>();
            }
        }

        public void LoadTargetScene()
        {
            if (isLoading)
            {
                return;
            }

            string resolvedSceneName = ResolveTargetSceneName();
            if (string.IsNullOrEmpty(resolvedSceneName))
            {
                Debug.LogError("[ModuleReturnToSceneButton] Hedef sahne build settings icinde bulunamadi.");
                return;
            }

            isLoading = true;
            if (sourceButton != null)
            {
                sourceButton.interactable = false;
            }

            Debug.Log("[ModuleReturnToSceneButton] Sahne yukleniyor: " + resolvedSceneName);
            XRSceneRuntimeStabilizer.PrepareForSceneTransition();
            XRCameraHelper.ClearCache();
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(resolvedSceneName, LoadSceneMode.Single);
            if (loadOperation == null)
            {
                isLoading = false;
                if (sourceButton != null)
                {
                    sourceButton.interactable = true;
                }

                Debug.LogError("[ModuleReturnToSceneButton] SceneManager.LoadSceneAsync null dondu.");
            }
        }

        private string ResolveTargetSceneName()
        {
            if (CanLoadScene(targetSceneName))
            {
                return targetSceneName;
            }

            string preferredKey = NormalizeSceneKey(string.IsNullOrWhiteSpace(targetSceneName) ? DefaultSceneName : targetSceneName);
            string fallbackKey = NormalizeSceneKey(DefaultSceneName);

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrWhiteSpace(scenePath))
                {
                    continue;
                }

                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                string sceneKey = NormalizeSceneKey(sceneName);
                if (sceneKey == preferredKey || sceneKey == fallbackKey)
                {
                    return sceneName;
                }
            }

            return string.Empty;
        }

        private static bool CanLoadScene(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);
        }

        private static string NormalizeSceneKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace("'", string.Empty)
                .Replace("\u0131", "i")
                .Replace("\u0130", "I")
                .Replace("\u00FC", "u")
                .Replace("\u00DC", "U")
                .Replace("\u00F6", "o")
                .Replace("\u00D6", "O")
                .Replace("\u00E7", "c")
                .Replace("\u00C7", "C")
                .Replace("\u011F", "g")
                .Replace("\u011E", "G")
                .Replace("\u015F", "s")
                .Replace("\u015E", "S")
                .ToUpperInvariant();
        }
    }
}
