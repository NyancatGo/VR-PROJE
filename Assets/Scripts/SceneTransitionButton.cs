using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionButton : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "Modul1"; // Sahne adını buraya yaz

    public void GoToScene()
    {
        XRSceneRuntimeStabilizer.PrepareForSceneTransition();
        XRCameraHelper.ClearCache();
        SceneManager.LoadScene(targetSceneName);
    }
}
