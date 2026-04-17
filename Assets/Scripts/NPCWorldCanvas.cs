using UnityEngine;

/// <summary>
/// Manages the visibility, smooth fade effect, and billboard (Look-At) behavior of the Canvas.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class NPCWorldCanvas : MonoBehaviour
{
    [Header("Billboard Settings")]
    public Transform playerCamera;
    
    [Header("UI Effects")]
    public float fadeSpeed = 8f;

    private CanvasGroup canvasGroup;
    private Canvas worldCanvas;
    private bool isVisible = false;

    // Static reference to ensure only ONE canvas is active globally at any time
    private static NPCWorldCanvas currentActiveCanvas;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        worldCanvas = GetComponent<Canvas>();
        
        // Hide canvas initially
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Optional check: automatic XR camera assignment
        if (playerCamera == null)
        {
            playerCamera = XRCameraHelper.GetPlayerCameraTransform();
        }

        if (worldCanvas != null && playerCamera != null)
        {
            worldCanvas.worldCamera = playerCamera.GetComponent<Camera>();
        }
    }

    private void Update()
    {
        // Smooth fade logic
        float targetAlpha = isVisible ? 1f : 0f;
        if (Mathf.Abs(canvasGroup.alpha - targetAlpha) > 0.01f)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }
    }

    private void LateUpdate()
    {
        if (!isVisible && canvasGroup.alpha <= 0.01f) return;

        // Billboard ve Konumlandırma mantığı
        if (playerCamera == null)
        {
            playerCamera = XRCameraHelper.GetPlayerCameraTransform();
            if (worldCanvas != null && playerCamera != null)
            {
                worldCanvas.worldCamera = playerCamera.GetComponent<Camera>();
            }
        }

        if (playerCamera != null)
        {
            transform.forward = transform.position - playerCamera.position;
        }
        
        // Eğer parent (Yaralı) yerde yatıyorsa (X rotasyonu bozulmuşsa), 
        // localPosition değişmese bile canvas karakterle beraber yatar veya zemine girer.
        // Bunu engellemek için canvası her zaman karakterin dünya konumundan 1.2 metre yukarıda sabitliyoruz.
        if (transform.parent != null)
        {
            transform.position = transform.parent.position + Vector3.up * 1.2f;
        }
    }

    /// <summary>
    /// Activates this NPC's canvas and hides any previously active canvas on other NPCs.
    /// </summary>
    public void ShowCanvas()
    {
        ShowCanvas(false);
    }

    public void ShowCanvas(bool forceDuringPlacementSequence)
    {
        if (!forceDuringPlacementSequence &&
            IlkyardimGlobalMenajer.Instance != null &&
            IlkyardimGlobalMenajer.Instance.IsPlacementSequenceActive)
        {
            return;
        }

        // If another NPC's canvas is currently visible, hide it.
        if (currentActiveCanvas != null && currentActiveCanvas != this)
        {
            currentActiveCanvas.HideCanvas();
        }

        currentActiveCanvas = this;
        isVisible = true;

        if (playerCamera == null)
        {
            playerCamera = XRCameraHelper.GetPlayerCameraTransform();
        }

        if (worldCanvas != null && playerCamera != null)
        {
            worldCanvas.worldCamera = playerCamera.GetComponent<Camera>();
        }

        // Enable UI interaction (for clicking VR buttons on the canvas)
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Trigger data binding to refresh dynamic text
        FirstAidUIBinder binder = GetComponent<FirstAidUIBinder>();
        if (binder != null)
        {
            binder.UpdateUI();
        }
    }

    /// <summary>
    /// Hides this NPC's canvas.
    /// </summary>
    public void HideCanvas()
    {
        isVisible = false;
        
        // Disable UI interaction immediately
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (currentActiveCanvas == this)
        {
            currentActiveCanvas = null;
        }
    }

    public void HideCanvasImmediate()
    {
        isVisible = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (currentActiveCanvas == this)
        {
            currentActiveCanvas = null;
        }
    }

    public static void HideAllCanvases()
    {
        NPCWorldCanvas[] canvases = FindObjectsOfType<NPCWorldCanvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null)
            {
                canvases[i].HideCanvasImmediate();
            }
        }
    }
}
