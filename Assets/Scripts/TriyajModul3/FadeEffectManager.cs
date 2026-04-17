using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;

namespace TriyajModul3
{
    /// <summary>
    /// VR uyumlu sinematik fade efekti.
    /// Kendi fade panelini runtime'da VR kameranın dibine yapıştırır.
    /// Inspector'dan fadePanel atamanıza GEREK YOK - otomatik oluşturur.
    /// </summary>
    public class FadeEffectManager : MonoBehaviour
    {
        public static FadeEffectManager Instance { get; private set; }

        [Header("Fade Panel (Opsiyonel)")]
        [Tooltip("Boş bırakabilirsiniz, otomatik oluşturulur.")]
        public Image fadePanel;

        [Header("Zamanlama (Saniye)")]
        [Tooltip("Kararma süresi")]
        public float fadeToBlackDuration = 0.5f;

        [Tooltip("Karanlıkta bekleme süresi")]
        public float stayBlackDuration = 0.8f;

        [Tooltip("Açılma süresi")]
        public float fadeFromBlackDuration = 0.7f;

        [Header("Animasyon Tipi")]
        public AnimationType animationType = AnimationType.Smooth;

        public enum AnimationType
        {
            Linear,
            Smooth,
            EaseInOut,
            SlowStartEnd
        }

        private Canvas fadeCanvas;
        private Transform vrCamera;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            StartCoroutine(InitFadePanel());
        }

        /// <summary>
        /// 1 frame bekleyip VR kamerayı bulur ve fade paneli oluşturur.
        /// </summary>
        private System.Collections.IEnumerator InitFadePanel()
        {
            // 1 frame bekle, XR Origin başlasın
            yield return null;

            // VR kamerayı bul
            vrCamera = FindVRCamera();

            if (vrCamera == null)
            {
                Debug.LogWarning("[FadeEffect] VR kamera bulunamadi, XR kamera helper fallback deneniyor...");
                vrCamera = XRCameraHelper.GetPlayerCameraTransform();
            }

            if (vrCamera == null)
            {
                Debug.LogError("[FadeEffect] Hiçbir kamera bulunamadı! Fade efekti çalışmayacak.");
                yield break;
            }

            // Eğer Inspector'dan fadePanel atanmamışsa, otomatik oluştur
            if (fadePanel == null)
            {
                CreateFadePanelOnCamera();
            }
            else
            {
                // Var olan paneli de kameraya bağla
                ParentExistingPanelToCamera();
            }

            // Başlangıçta saydam
            SetPanelAlpha(0f);
            Debug.Log("[FadeEffect] Fade paneli VR kameraya bağlandı. Kamera: " + vrCamera.name);
        }

        private Transform FindVRCamera()
        {
            XROrigin origin = XRCameraHelper.GetXROrigin();
            if (origin != null && origin.Camera != null)
            {
                return origin.Camera.transform;
            }
            return XRCameraHelper.GetPlayerCameraTransform();
        }

        /// <summary>
        /// Sıfırdan fade canvas + image oluşturup kameranın 0.15m önüne yapıştırır.
        /// </summary>
        private void CreateFadePanelOnCamera()
        {
            // Canvas oluştur
            GameObject canvasObj = new GameObject("__FadeOverlay");
            canvasObj.transform.SetParent(vrCamera, false);
            canvasObj.layer = 5; // UI layer

            fadeCanvas = canvasObj.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            // Kameranın hemen dibinde küçük bir kare (0.15m önde)
            canvasObj.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            canvasRect.sizeDelta = new Vector2(2000f, 2000f);

            // Siyah Image oluştur
            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvasObj.transform, false);
            imageObj.layer = 5;

            fadePanel = imageObj.AddComponent<Image>();
            fadePanel.color = new Color(0f, 0f, 0f, 0f); // Başlangıçta saydam
            fadePanel.raycastTarget = false;

            RectTransform imageRect = imageObj.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            Debug.Log("[FadeEffect] Fade paneli oluşturuldu ve kameraya bağlandı.");
        }

        /// <summary>
        /// Inspector'dan atanmış var olan paneli kameranın önüne taşır.
        /// </summary>
        private void ParentExistingPanelToCamera()
        {
            Canvas existingCanvas = fadePanel.GetComponentInParent<Canvas>();
            if (existingCanvas != null)
            {
                existingCanvas.renderMode = RenderMode.WorldSpace;
                existingCanvas.transform.SetParent(vrCamera, false);
                existingCanvas.transform.localPosition = new Vector3(0f, 0f, 0.15f);
                existingCanvas.transform.localRotation = Quaternion.identity;
                existingCanvas.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

                RectTransform canvasRect = existingCanvas.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(2000f, 2000f);

                // Image'ı tam ekran yap
                RectTransform imageRect = fadePanel.GetComponent<RectTransform>();
                imageRect.anchorMin = Vector2.zero;
                imageRect.anchorMax = Vector2.one;
                imageRect.offsetMin = Vector2.zero;
                imageRect.offsetMax = Vector2.zero;

                fadeCanvas = existingCanvas;
            }
        }

        private void SetPanelAlpha(float alpha)
        {
            if (fadePanel != null)
            {
                Color c = fadePanel.color;
                c.a = alpha;
                fadePanel.color = c;
            }
        }

        private float GetAnimatedValue(float t)
        {
            switch (animationType)
            {
                case AnimationType.Linear:
                    return t;

                case AnimationType.Smooth:
                    return t * t * (3f - 2f * t);

                case AnimationType.EaseInOut:
                    return t < 0.5f
                        ? 2f * t * t
                        : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

                case AnimationType.SlowStartEnd:
                    return t * t * (3f - 2f * t) * 0.5f + t * 0.5f;

                default:
                    return t;
            }
        }

        /// <summary>
        /// Sadece kararmayı başlatır ve Coroutine döner
        /// </summary>
        public System.Collections.IEnumerator StartFadeToBlackCoroutine()
        {
            yield return StartCoroutine(FadeToBlackRoutine(null));
        }

        private System.Collections.IEnumerator FadeToBlackRoutine(System.Action onComplete)
        {
            float elapsed = 0f;

            while (elapsed < fadeToBlackDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeToBlackDuration);
                float animatedT = GetAnimatedValue(t);

                SetPanelAlpha(animatedT);
                yield return null;
            }

            SetPanelAlpha(1f);
            onComplete?.Invoke();
        }

        /// <summary>
        /// Sadece açılmayı başlatır ve Coroutine döner
        /// </summary>
        public System.Collections.IEnumerator StartFadeFromBlackCoroutine()
        {
            yield return StartCoroutine(FadeFromBlackRoutine(null));
        }

        private System.Collections.IEnumerator FadeFromBlackRoutine(System.Action onComplete)
        {
            yield return new WaitForSeconds(stayBlackDuration);

            float elapsed = 0f;

            while (elapsed < fadeFromBlackDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeFromBlackDuration);
                float animatedT = GetAnimatedValue(t);

                float alpha = 1f - animatedT;
                SetPanelAlpha(alpha);

                yield return null;
            }

            SetPanelAlpha(0f);
            onComplete?.Invoke();
        }

        /// <summary>
        /// Karar ve açılma efektini tamamen çalıştırır
        /// </summary>
        public void DoFullFadeSequence(System.Action onSequenceComplete = null)
        {
            StartCoroutine(FullFadeSequence(onSequenceComplete));
        }

        private System.Collections.IEnumerator FullFadeSequence(System.Action onSequenceComplete)
        {
            yield return StartCoroutine(FadeToBlackRoutine(null));
            yield return new WaitForSeconds(stayBlackDuration);
            yield return StartCoroutine(FadeFromBlackRoutine(null));
            onSequenceComplete?.Invoke();
        }

        /// <summary>
        /// Anlık karartma
        /// </summary>
        public void InstantFadeToBlack()
        {
            SetPanelAlpha(1f);
        }

        /// <summary>
        /// Anında açılma
        /// </summary>
        public void InstantFadeClear()
        {
            SetPanelAlpha(0f);
        }

        private void OnDestroy()
        {
            // Clear overlay and release singleton on scene unload so the next scene
            // doesn't inherit a stale reference pointing at a destroyed panel.
            InstantFadeClear();
            if (Instance == this)
                Instance = null;
        }
    }
}
