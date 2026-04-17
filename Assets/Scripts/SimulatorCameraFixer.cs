using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class SimulatorCameraFixer : MonoBehaviour
{
    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene Scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        var sim = GetComponent<XRDeviceSimulator>();
        if (sim != null)
        {
            var rigCam = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Main Camera");
            if (rigCam == null) rigCam = GameObject.Find("XR Origin/Camera Offset/Main Camera");
            
            if (rigCam != null)
            {
                sim.cameraTransform = rigCam.transform;
                Debug.Log($"[SimulatorCameraFixer] Automatically assigned '{rigCam.name}' to XR Device Simulator.");
            }
        }
    }
    
    void Start()
    {
        var sim = GetComponent<XRDeviceSimulator>();
        if (sim != null && sim.cameraTransform == null)
        {
            var rigCam = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Main Camera");
            if (rigCam == null) rigCam = GameObject.Find("XR Origin/Camera Offset/Main Camera");
            
            if (rigCam != null)
            {
                sim.cameraTransform = rigCam.transform;
                Debug.Log($"[SimulatorCameraFixer] Assigned '{rigCam.name}' to XR Device Simulator on Start.");
            }
        }
    }
}