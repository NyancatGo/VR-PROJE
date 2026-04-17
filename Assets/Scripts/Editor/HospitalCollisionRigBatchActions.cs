#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HospitalCollisionRigBatchActions
{
    private const string Modul3TriyajScenePath =
        "Assets/Samples/XR Interaction Toolkit/2.6.5/XR Device Simulator/Scenes/Mod\u00FCl3_Triyaj.unity";

    public static void RebuildModul3TriageScene()
    {
        var scene = EditorSceneManager.OpenScene(Modul3TriyajScenePath, OpenSceneMode.Single);
        var rigs = UnityEngine.Object.FindObjectsOfType<HospitalCollisionRig>(true);
        if (rigs == null || rigs.Length == 0)
        {
            throw new InvalidOperationException(
                $"No {nameof(HospitalCollisionRig)} found in scene '{Modul3TriyajScenePath}'.");
        }

        for (int i = 0; i < rigs.Length; i++)
        {
            var rig = rigs[i];
            rig.RebuildHospitalCollisionRig();
            EditorUtility.SetDirty(rig);

            if (rig.gameObject.scene == scene)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException($"Failed to save scene '{Modul3TriyajScenePath}'.");
        }
    }
}
#endif
