using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Modul1 sahnesindeki kartlara runtime'da "Aç" butonu ekler ve içeriği
    /// Unity içindeki ContentViewer panelinde açar.
    /// </summary>
    public static class Module1OpenButtonBootstrap
    {
        private const string TargetSceneName = "Modul1";
        private static readonly Dictionary<string, string> TextCorrections = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Deprem Acil Durum Plani", "Deprem Acil Durum Planı" },
            { "Acil durum planlama adimlari ve uygulama noktalari", "Acil durum planlama adımları ve uygulama noktaları" },
            { "Deprem Cantasi Icerigi", "Deprem Çantası İçeriği" },
            { "Deprem cantasinda bulunmasi gereken temel ekipmanlar", "Deprem çantasında bulunması gereken temel ekipmanlar" },
            { "Toplanma ve Tahliye Rehberi", "Toplanma ve Tahliye Rehberi" },
            { "Toplanma alani secimi ve guvenli tahliye kurallari", "Toplanma alanı seçimi ve güvenli tahliye kuralları" }
        };

        private const int InfografikContentType = 0;
        private const int SunumContentType = 1;
        private const int VideoContentType = 2;

        // NOT: Kartlar artık ContentLoader tarafından dinamik oluşturuluyor.
        // Bu eski bootstrap devre dışı bırakıldı.
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            // ContentLoader tüm kartları ve butonları dinamik olarak oluşturduğu için
            // bu eski yöntem artık kullanılmıyor.
            return;
        }

        private static void TryCreateButtons()
        {
            NormalizeVisibleTexts();

            var roots = new List<Transform>();
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                roots.Add(root.transform);
            }

            var infografikPanel = FindFirstByName(roots, "Infografikler");
            var sunumPanel = FindFirstByName(roots, "SunumlarPanel");
            var videoPanel = FindFirstByName(roots, "VideolarPanel");

            AddButtonsToPanel(infografikPanel, "Card_", InfografikContentType);
            AddButtonsToPanel(sunumPanel, "Card_", SunumContentType);
            AddButtonsToPanel(videoPanel, "Card_", VideoContentType);
        }

        private static void NormalizeVisibleTexts()
        {
            foreach (var tmp in UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>(true))
            {
                if (tmp == null || string.IsNullOrEmpty(tmp.text))
                {
                    continue;
                }

                if (TextCorrections.TryGetValue(tmp.text, out var correctedText))
                {
                    tmp.text = correctedText;
                }
            }

            foreach (var text in UnityEngine.Object.FindObjectsOfType<Text>(true))
            {
                if (text == null || string.IsNullOrEmpty(text.text))
                {
                    continue;
                }

                if (TextCorrections.TryGetValue(text.text, out var correctedText))
                {
                    text.text = correctedText;
                }
            }
        }

        private static void AddButtonsToPanel(Transform panel, string cardPrefix, int contentType)
        {
            if (panel == null)
            {
                return;
            }

            for (int i = 1; i <= 3; i++)
            {
                var card = panel.Find($"{cardPrefix}{i}");
                if (card == null)
                {
                    continue;
                }

                var openButton = card.Find("OpenButton");
                if (openButton == null)
                {
                    openButton = CreateOpenButton(card);
                }

                var button = openButton.GetComponent<Button>();
                if (button == null)
                {
                    button = openButton.gameObject.AddComponent<Button>();
                }

                var idx = i - 1;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OpenInProjectViewer(contentType, idx));
            }
        }

        private static Transform CreateOpenButton(Transform card)
        {
            var buttonGo = new GameObject("OpenButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            buttonGo.transform.SetParent(card, false);

            var rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-18f, 18f);
            rect.sizeDelta = new Vector2(128f, 44f);

            var image = buttonGo.GetComponent<Image>();
            image.color = new Color(0f, 0.56f, 0.72f, 0.9f);

            var outline = buttonGo.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0.9f, 1f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(buttonGo.transform, false);

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            // Karttaki mevcut TMP fontunu kullanarak Türkçe karakter desteğini koru.
            var existingTmp = card.GetComponentInChildren<TextMeshProUGUI>();
            if (existingTmp != null)
            {
                tmp.font = existingTmp.font;
            }

            tmp.text = "Aç";
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false;

            return buttonGo.transform;
        }

        private static void OpenInProjectViewer(int contentType, int index)
        {
            var manager = UnityEngine.Object.FindObjectOfType<ContentViewerManager>(true);
            if (manager == null)
            {
                Debug.LogWarning("ContentViewerManager bulunamadı. İçerik paneli açılamadı.");
                return;
            }

            var safeIndex = Mathf.Max(0, index);
            manager.ShowContent(contentType, safeIndex);
        }

        private static Transform FindFirstByName(IEnumerable<Transform> roots, string name)
        {
            foreach (var root in roots)
            {
                var found = FindDeepChild(root, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == name)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var result = FindDeepChild(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
