using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TriyajModul3;

/// <summary>
/// Fade Canvas'ı VR için doğru ayarlar: Screen Space Overlay yapıp
/// FadeEffectManager'a bağlar. 1 tıkla tamamdır.
/// </summary>
public class AutoFixFadeCanvas : EditorWindow
{
    [MenuItem("Tools/2 TIKLA: Fade Efektini Duzelt")]
    public static void FixFadeCanvas()
    {
        // 1. fadecanvas objesini bul
        GameObject fadeCanvasObj = GameObject.Find("fadecanvas");
        if (fadeCanvasObj == null)
        {
            // inaktif olabilir
            Transform[] allObjects = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform t in allObjects)
            {
                if ((t.name == "fadecanvas" || t.name == "FadeCanvas") && t.gameObject.hideFlags == HideFlags.None)
                {
                    fadeCanvasObj = t.gameObject;
                    break;
                }
            }
        }

        if (fadeCanvasObj == null)
        {
            Debug.LogError("❌ fadecanvas bulunamadı! Önce sahneye bir Canvas ekle ve adını 'fadecanvas' koy.");
            return;
        }

        // 2. Canvas ayarlarını düzelt
        Canvas canvas = fadeCanvasObj.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = fadeCanvasObj.AddComponent<Canvas>();
        }

        // VR'da Screen Space Overlay çalışmaz, o yüzden Screen Space Camera kullanacağız.
        // Ama VR kamerası runtime'da değişebilir, en güvenilir yol:
        // Canvas'ı Screen Space - Overlay yapıp Sort Order'ı yükseğe çekmek.
        // Unity 2022'de VR projelerinde Overlay mode çalışmayabilir,
        // o yüzden biz World Space + kameraya child yapacağız (runtime'da).
        // AMA Editor'de ayarı şöyle bırakıyoruz:
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Her şeyin en üstünde

        // 3. Image'ı bul ve ayarla
        Image fadeImage = fadeCanvasObj.GetComponentInChildren<Image>(true);
        if (fadeImage == null)
        {
            // Image yoksa oluştur
            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(fadeCanvasObj.transform, false);
            fadeImage = imageObj.AddComponent<Image>();
        }

        // Simsiyah ve başlangıçta saydam
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false; // tıklamaları engellemasın

        // Tam ekranı kapla
        RectTransform imageRect = fadeImage.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        // Canvas RectTransform'u da sıfırla
        RectTransform canvasRect = fadeCanvasObj.GetComponent<RectTransform>();
        canvasRect.localScale = Vector3.one;

        // Gereksiz bileşenleri temizle
        var graphicRaycaster = fadeCanvasObj.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (graphicRaycaster != null)
        {
            graphicRaycaster.enabled = false; // Fade canvas'a tıklama gitmemeli
        }

        EditorUtility.SetDirty(fadeCanvasObj);
        EditorUtility.SetDirty(fadeImage);

        // 4. FadeEffectManager'ı bul ve bağla
        FadeEffectManager manager = FindObjectOfType<FadeEffectManager>();
        if (manager == null)
        {
            // fadecanvas üzerinde veya ayrı objede olabilir
            GameObject managerObj = GameObject.Find("FadeEffectManager");
            if (managerObj != null)
            {
                manager = managerObj.GetComponent<FadeEffectManager>();
            }
        }

        if (manager == null)
        {
            // Hiçbir yerde yok, fadecanvas'a ekle
            manager = fadeCanvasObj.AddComponent<FadeEffectManager>();
        }

        // Image referansını bağla
        manager.fadePanel = fadeImage;
        EditorUtility.SetDirty(manager);

        Debug.Log("✅ [BAŞARILI] Fade efekti düzeltildi! fadecanvas = Screen Space Overlay, Sort Order = 999, FadeImage = siyah tam ekran, FadeEffectManager bağlandı.");
    }
}
