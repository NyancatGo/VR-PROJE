using UnityEngine;
using UnityEngine.SceneManagement;

public static class XRCameraHelper
{
    private static Camera _cachedCamera;
    private static Unity.XR.CoreUtils.XROrigin _cachedOrigin;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnSubsystemRegistration()
    {
        ClearCache();
    }

    public static Camera GetPlayerCamera()
    {
        if (_cachedCamera != null && !_cachedCamera.Equals(null) && _cachedCamera.gameObject.activeInHierarchy)
        {
            return _cachedCamera;
        }

        _cachedCamera = null;

        _cachedOrigin = FindPreferredOrigin();
        if (_cachedOrigin != null && _cachedOrigin.Camera != null)
        {
            _cachedCamera = _cachedOrigin.Camera;
            return _cachedCamera;
        }

        _cachedCamera = FindPreferredCamera();
        return _cachedCamera;
    }

    public static Transform GetPlayerCameraTransform()
    {
        return GetPlayerCamera()?.transform;
    }

    public static Unity.XR.CoreUtils.XROrigin GetXROrigin()
    {
        if (_cachedOrigin != null && !_cachedOrigin.Equals(null) && _cachedOrigin.gameObject.activeInHierarchy)
        {
            return _cachedOrigin;
        }

        _cachedOrigin = null;

        _cachedOrigin = FindPreferredOrigin();
        return _cachedOrigin;
    }

    public static void ClearCache()
    {
        _cachedCamera = null;
        _cachedOrigin = null;
    }

    private static Unity.XR.CoreUtils.XROrigin FindPreferredOrigin()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.isLoaded)
        {
            GameObject[] roots = activeScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] == null)
                {
                    continue;
                }

                Unity.XR.CoreUtils.XROrigin sceneOrigin =
                    roots[i].GetComponentInChildren<Unity.XR.CoreUtils.XROrigin>(false);
                if (sceneOrigin != null && sceneOrigin.gameObject.activeInHierarchy)
                {
                    return sceneOrigin;
                }
            }
        }

        Unity.XR.CoreUtils.XROrigin[] origins =
            UnityEngine.Object.FindObjectsOfType<Unity.XR.CoreUtils.XROrigin>(true);
        for (int i = 0; i < origins.Length; i++)
        {
            Unity.XR.CoreUtils.XROrigin origin = origins[i];
            if (origin != null && origin.gameObject.activeInHierarchy)
            {
                return origin;
            }
        }

        return null;
    }

    private static Camera FindPreferredCamera()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>(true);

        Camera activeSceneFallback = null;
        Camera globalFallback = null;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null || !camera.gameObject.activeInHierarchy)
            {
                continue;
            }

            bool inActiveScene = activeScene.IsValid() && camera.gameObject.scene == activeScene;
            if (inActiveScene && camera.CompareTag("MainCamera"))
            {
                return camera;
            }

            if (inActiveScene && activeSceneFallback == null)
            {
                activeSceneFallback = camera;
            }
            else if (globalFallback == null)
            {
                globalFallback = camera;
            }
        }

        return activeSceneFallback != null ? activeSceneFallback : globalFallback;
    }
}
