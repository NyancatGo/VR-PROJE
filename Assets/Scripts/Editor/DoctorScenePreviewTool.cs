using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DoctorScenePreviewTool
{
    private const string SceneDoctorName = "Lowpoly_Doctor";
    private const string PreviewClipPath = "Assets/BackRock Studios/LowPoly-Doctor/Animations/Doctor_IdleStabilized.anim";

    static DoctorScenePreviewTool()
    {
        EditorApplication.update -= UpdatePreview;
        EditorApplication.update += UpdatePreview;
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
        {
            StopPreview();
        }
    }

    private static void UpdatePreview()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            StopPreview();
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            StopPreview();
            return;
        }

        GameObject doctorObject = GameObject.Find(SceneDoctorName);
        AnimationClip previewClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(PreviewClipPath);
        if (doctorObject == null || previewClip == null || previewClip.length <= 0.01f)
        {
            StopPreview();
            return;
        }

        if (!AnimationMode.InAnimationMode())
        {
            AnimationMode.StartAnimationMode();
        }

        float previewTime = (float)(EditorApplication.timeSinceStartup % previewClip.length);

        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(doctorObject, previewClip, previewTime);
        AnimationMode.EndSampling();

        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.Repaint();
        }
    }

    private static void StopPreview()
    {
        if (AnimationMode.InAnimationMode())
        {
            AnimationMode.StopAnimationMode();
        }
    }
}
