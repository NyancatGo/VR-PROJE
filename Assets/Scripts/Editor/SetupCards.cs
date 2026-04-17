using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SetupCards : MonoBehaviour
{
    [ContextMenu("Setup All Cards")]
    public void SetupAllCards()
    {
        SetupCard("sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/SunumlarPanel/SunumCard_1", "Sunum 1", "Sunum açıklaması");
        SetupCard("sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/SunumlarPanel/SunumCard_2", "Sunum 2", "Sunum açıklaması");
        SetupCard("sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/SunumlarPanel/SunumCard_3", "Sunum 3", "Sunum açıklaması");
        SetupCard("sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/VideolarPanel/VideoCard_1", "Video 1", "Video açıklaması");
        SetupCard("sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/VideolarPanel/VideoCard_2", "Video 2", "Video açıklaması");
        SetupCard("sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/VideolarPanel/VideoCard_3", "Video 3", "Video açıklaması");
        Debug.Log("Cards setup completed!");
    }

    private void SetupCard(string cardPath, string title, string description)
    {
        GameObject card = GameObject.Find(cardPath);
        if (card == null)
        {
            Debug.LogError("Card not found: " + cardPath);
            return;
        }

        // Add Outline
        Outline outline = card.GetComponent<Outline>();
        if (outline == null)
        {
            outline = card.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0f, 0.898f, 1f, 0.25f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Setup RectTransform
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = new Vector2(520, 740);

        // Create Thumbnail
        CreateThumbnail(card.transform);

        // Create Title
        CreateTitle(card.transform, title);

        // Create Description
        CreateDescription(card.transform, description);
    }

    private void CreateThumbnail(Transform parent)
    {
        GameObject thumb = new GameObject("Thumbnail");
        thumb.transform.SetParent(parent);

        RectTransform rect = thumb.AddComponent<RectTransform>();
        Image img = thumb.AddComponent<Image>();
        img.color = Color.white;

        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, 465);
    }

    private void CreateTitle(Transform parent, string text)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent);

        RectTransform rect = titleObj.AddComponent<RectTransform>();
        titleObj.AddComponent<CanvasRenderer>();
        TextMeshProUGUI textComp = titleObj.AddComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.fontSize = 28;
        textComp.fontStyle = FontStyles.Bold;
        textComp.alignment = TextAlignmentOptions.TopLeft;
        textComp.color = Color.white;

        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(18, -479);
        rect.sizeDelta = new Vector2(-36, 40);
    }

    private void CreateDescription(Transform parent, string text)
    {
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(parent);

        RectTransform rect = descObj.AddComponent<RectTransform>();
        descObj.AddComponent<CanvasRenderer>();
        TextMeshProUGUI textComp = descObj.AddComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.fontSize = 22;
        textComp.alignment = TextAlignmentOptions.TopLeft;
        textComp.color = new Color(0.7f, 0.7f, 0.7f, 1f);

        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(18, -530);
        rect.sizeDelta = new Vector2(-36, 150);
    }
}