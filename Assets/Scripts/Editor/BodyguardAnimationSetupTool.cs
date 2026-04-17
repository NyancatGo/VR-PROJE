using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BodyguardAnimationSetupTool
{
    private const string StabilizedControllerPath = "Assets/BodyGuards/Meshes/Bodyguard_IdleStabilized.controller";
    private const string StabilizedClipPath = "Assets/BackRock Studios/LowPoly-Doctor/Animations/Doctor_IdleStabilized.anim";
    private const string SceneBodyguardName = "SkelMesh_Bodyguard_01";
    private const string BodyguardModelPath = "Assets/BodyGuards/Meshes/SkelMesh_Bodyguard_01.fbx";

    [MenuItem("Tools/Triyaj Hastane/Bodyguard Idle Animasyonunu Bagla")]
    public static void SetupBodyguardIdleAnimation()
    {
        RuntimeAnimatorController controller = EnsureBodyguardIdleController();
        Avatar avatar = LoadBodyguardAvatar();

        if (controller == null)
        {
            Debug.LogError("[BodyguardAnimationSetupTool] Bodyguard controller bulunamadi.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            Debug.LogWarning("[BodyguardAnimationSetupTool] Gecerli aktif sahne bulunamadi.");
            return;
        }

        GameObject bodyguardObject = FindSceneBodyguard();
        if (bodyguardObject == null)
        {
            Debug.LogWarning("[BodyguardAnimationSetupTool] Aktif sahnede bodyguard bulunamadi.");
            return;
        }

        Animator animator = bodyguardObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = bodyguardObject.GetComponentInChildren<Animator>(true);
        }

        if (animator == null)
        {
            Debug.LogWarning("[BodyguardAnimationSetupTool] Bodyguard animator bulunamadi.");
            return;
        }

        Transform rootTransform = bodyguardObject.transform;
        Vector3 lockedPosition = rootTransform.position;
        Quaternion lockedRotation = rootTransform.rotation;
        Vector3 lockedScale = rootTransform.localScale;

        bool changed = ConfigureAnimator(animator, controller, avatar);

        animator.Rebind();
        animator.Update(0f);

        if (rootTransform.position != lockedPosition)
        {
            rootTransform.position = lockedPosition;
            changed = true;
        }

        if (rootTransform.rotation != lockedRotation)
        {
            rootTransform.rotation = lockedRotation;
            changed = true;
        }

        if (rootTransform.localScale != lockedScale)
        {
            rootTransform.localScale = lockedScale;
            changed = true;
        }

        if (!changed)
        {
            Debug.Log("[BodyguardAnimationSetupTool] Bodyguard animator zaten bagliydi.");
            return;
        }

        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        PrefabUtility.RecordPrefabInstancePropertyModifications(rootTransform);
        EditorUtility.SetDirty(animator);
        EditorUtility.SetDirty(rootTransform);
        EditorSceneManager.MarkSceneDirty(activeScene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BodyguardAnimationSetupTool] Bodyguard idle animasyonu konum korunarak baglandi.");
    }

    private static RuntimeAnimatorController EnsureBodyguardIdleController()
    {
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(StabilizedClipPath);
        if (idleClip == null)
        {
            return null;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(StabilizedControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(StabilizedControllerPath);
        }

        if (controller == null || controller.layers.Length == 0)
        {
            return null;
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = null;

        ChildAnimatorState[] states = stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state != null && states[i].state.name == "Idle")
            {
                idleState = states[i].state;
                break;
            }
        }

        if (idleState == null)
        {
            idleState = stateMachine.AddState("Idle");
        }

        idleState.motion = idleClip;
        stateMachine.defaultState = idleState;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static Avatar LoadBodyguardAvatar()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(BodyguardModelPath);
        for (int i = 0; i < assets.Length; i++)
        {
            Avatar avatar = assets[i] as Avatar;
            if (avatar != null)
            {
                return avatar;
            }
        }

        return null;
    }

    private static bool ConfigureAnimator(Animator animator, RuntimeAnimatorController controller, Avatar avatar)
    {
        bool changed = false;
        Undo.RecordObject(animator, "Setup bodyguard idle animation");

        if (animator.runtimeAnimatorController != controller)
        {
            animator.runtimeAnimatorController = controller;
            changed = true;
        }

        if (avatar != null && animator.avatar != avatar)
        {
            animator.avatar = avatar;
            changed = true;
        }

        if (animator.applyRootMotion)
        {
            animator.applyRootMotion = false;
            changed = true;
        }

        if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            changed = true;
        }

        if (animator.updateMode != AnimatorUpdateMode.Normal)
        {
            animator.updateMode = AnimatorUpdateMode.Normal;
            changed = true;
        }

        return changed;
    }

    private static GameObject FindSceneBodyguard()
    {
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject != null && selectedObject.name == SceneBodyguardName)
        {
            return selectedObject;
        }

        return GameObject.Find(SceneBodyguardName);
    }
}
