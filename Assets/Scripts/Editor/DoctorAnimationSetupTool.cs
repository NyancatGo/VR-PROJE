using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DoctorAnimationSetupTool
{
    private const string ControllerPath = "Assets/BackRock Studios/LowPoly-Doctor/Animations/Doctor_AnimController.controller";
    private const string SourceIdleClipPath = "Assets/BackRock Studios/LowPoly-Doctor/Animations/idle.anim";
    private const string StabilizedIdleClipPath = "Assets/BackRock Studios/LowPoly-Doctor/Animations/Doctor_IdleStabilized.anim";
    private const string StabilizedControllerPath = "Assets/BackRock Studios/LowPoly-Doctor/Animations/Doctor_IdleStabilized.controller";
    private const string PrefabPath = "Assets/BackRock Studios/LowPoly-Doctor/Prefab/Lowpoly_Doctor.prefab";
    private const string ModelPath = "Assets/BackRock Studios/LowPoly-Doctor/3D-models/Lowpoly_Doctor.fbx";
    private const string SceneDoctorName = "Lowpoly_Doctor";
    private const string DoctorLabelAnchorName = "Doctor_LabelAnchor";
    private static readonly Vector3 SceneDoctorWorldPosition = new Vector3(1.2f, 1.8600001f, 14f);
    private static readonly Vector3 SceneDoctorEulerAngles = new Vector3(0f, 178.715f, 0f);
    private static readonly Vector3 SceneDoctorScale = new Vector3(5.9356f, 5.9356f, 5.9356f);
    private static readonly Vector3 DoctorLabelAnchorLocalPosition = new Vector3(0.123f, 0.31f, -0.257f);
    private static readonly Vector3 DoctorLabelAnchorLocalEulerAngles = new Vector3(0f, 180f, 0f);
    private const float AlignmentTolerance = 0.0005f;
    private static readonly string[] RootTranslationProperties =
    {
        "RootT.x",
        "RootT.y",
        "RootT.z"
    };

    [MenuItem("Tools/Triyaj Hastane/Doktor Idle Animasyonunu Bagla")]
    public static void SetupDoctorIdleAnimation()
    {
        RuntimeAnimatorController controller = EnsureStabilizedIdleController();
        Avatar avatar = LoadDoctorAvatar();

        if (controller == null)
        {
            Debug.LogError("[DoctorAnimationSetupTool] Doctor controller bulunamadi.");
            return;
        }

        int updatedTargetCount = 0;

        if (TryConfigureSceneDoctor(controller, avatar))
        {
            updatedTargetCount++;
        }

        if (TryConfigureDoctorPrefab(controller, avatar))
        {
            updatedTargetCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (updatedTargetCount == 0)
        {
            Debug.Log("[DoctorAnimationSetupTool] Doktor animator zaten bagliydi veya hedef bulunamadi.");
            return;
        }

        Debug.Log($"[DoctorAnimationSetupTool] Doktor idle animasyonu ve hiza duzeltmesi {updatedTargetCount} hedefte uygulandi.");
    }

    private static bool TryConfigureSceneDoctor(RuntimeAnimatorController controller, Avatar avatar)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            return false;
        }

        GameObject doctorObject = FindSceneDoctor();
        if (doctorObject == null)
        {
            return false;
        }

        Animator animator = doctorObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = doctorObject.GetComponentInChildren<Animator>(true);
        }

        if (animator == null)
        {
            Debug.LogWarning("[DoctorAnimationSetupTool] Aktif sahnede Animator bulunan doktor bulunamadi.");
            return false;
        }

        bool changed = ConfigureAnimator(animator, controller, avatar);
        changed |= AlignSceneDoctor(doctorObject);

        if (!changed)
        {
            return false;
        }

        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        EditorUtility.SetDirty(animator);
        EditorSceneManager.MarkSceneDirty(activeScene);
        animator.Rebind();
        animator.Update(0f);
        return true;
    }

    private static bool TryConfigureDoctorPrefab(RuntimeAnimatorController controller, Avatar avatar)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (prefabRoot == null)
        {
            return false;
        }

        try
        {
            Animator animator = prefabRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = prefabRoot.GetComponentInChildren<Animator>(true);
            }

            if (animator == null)
            {
                Debug.LogWarning("[DoctorAnimationSetupTool] Lowpoly_Doctor prefabinda Animator bulunamadi.");
                return false;
            }

            bool changed = ConfigureAnimator(animator, controller, avatar);
            if (!changed)
            {
                return false;
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static bool ConfigureAnimator(Animator animator, RuntimeAnimatorController controller, Avatar avatar)
    {
        if (animator == null || controller == null)
        {
            return false;
        }

        bool changed = false;
        Undo.RecordObject(animator, "Setup doctor idle animation");

        if (animator.runtimeAnimatorController != controller)
        {
            animator.runtimeAnimatorController = controller;
            changed = true;
        }

        if (animator.avatar == null && avatar != null)
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

    private static bool AlignSceneDoctor(GameObject doctorObject)
    {
        if (doctorObject == null)
        {
            return false;
        }

        bool changed = false;
        Transform doctorTransform = doctorObject.transform;

        Undo.RecordObject(doctorTransform, "Align doctor transform");

        if (!Approximately(doctorTransform.position, SceneDoctorWorldPosition))
        {
            doctorTransform.position = SceneDoctorWorldPosition;
            changed = true;
        }

        if (!Approximately(doctorTransform.localScale, SceneDoctorScale))
        {
            doctorTransform.localScale = SceneDoctorScale;
            changed = true;
        }

        if (!ApproximatelyEuler(doctorTransform.eulerAngles, SceneDoctorEulerAngles))
        {
            doctorTransform.rotation = Quaternion.Euler(SceneDoctorEulerAngles);
            changed = true;
        }

        if (changed)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(doctorTransform);
            EditorUtility.SetDirty(doctorTransform);
        }

        Transform labelAnchor = FindChildRecursive(doctorTransform, DoctorLabelAnchorName);
        if (labelAnchor != null)
        {
            bool labelChanged = false;
            Undo.RecordObject(labelAnchor, "Align doctor label anchor");

            if (!Approximately(labelAnchor.localPosition, DoctorLabelAnchorLocalPosition))
            {
                labelAnchor.localPosition = DoctorLabelAnchorLocalPosition;
                changed = true;
                labelChanged = true;
            }

            if (!ApproximatelyEuler(labelAnchor.localEulerAngles, DoctorLabelAnchorLocalEulerAngles))
            {
                labelAnchor.localRotation = Quaternion.Euler(DoctorLabelAnchorLocalEulerAngles);
                changed = true;
                labelChanged = true;
            }

            if (labelChanged)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(labelAnchor);
                EditorUtility.SetDirty(labelAnchor);
            }
        }

        return changed;
    }

    private static Avatar LoadDoctorAvatar()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
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

    private static RuntimeAnimatorController EnsureStabilizedIdleController()
    {
        AnimationClip sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SourceIdleClipPath);
        if (sourceClip == null)
        {
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        }

        AnimationClip stabilizedClip = EnsureStabilizedIdleClip(sourceClip);
        if (stabilizedClip == null)
        {
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(StabilizedControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(StabilizedControllerPath);
        }

        if (controller == null || controller.layers.Length == 0)
        {
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
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

        idleState.motion = stabilizedClip;
        stateMachine.defaultState = idleState;
        EditorUtility.SetDirty(controller);

        return controller;
    }

    private static AnimationClip EnsureStabilizedIdleClip(AnimationClip sourceClip)
    {
        AnimationClip stabilizedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(StabilizedIdleClipPath);
        if (stabilizedClip == null)
        {
            if (!AssetDatabase.CopyAsset(SourceIdleClipPath, StabilizedIdleClipPath))
            {
                return null;
            }

            stabilizedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(StabilizedIdleClipPath);
        }

        if (stabilizedClip == null)
        {
            return null;
        }

        EditorCurveBinding[] floatBindings = AnimationUtility.GetCurveBindings(sourceClip);
        for (int i = 0; i < floatBindings.Length; i++)
        {
            EditorCurveBinding binding = floatBindings[i];
            AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            AnimationUtility.SetEditorCurve(stabilizedClip, binding, sourceCurve);
        }

        EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(sourceClip);
        for (int i = 0; i < objectBindings.Length; i++)
        {
            EditorCurveBinding binding = objectBindings[i];
            ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
            AnimationUtility.SetObjectReferenceCurve(stabilizedClip, binding, keyframes);
        }

        float clipLength = Mathf.Max(sourceClip.length, 0.01f);

        for (int i = 0; i < RootTranslationProperties.Length; i++)
        {
            SetRootCurveValue(stabilizedClip, RootTranslationProperties[i], 0f, clipLength);
        }

        SetRootCurveValue(stabilizedClip, "RootQ.x", 0f, clipLength);
        SetRootCurveValue(stabilizedClip, "RootQ.y", 0f, clipLength);
        SetRootCurveValue(stabilizedClip, "RootQ.z", 0f, clipLength);
        SetRootCurveValue(stabilizedClip, "RootQ.w", 1f, clipLength);

        EditorUtility.SetDirty(stabilizedClip);
        return stabilizedClip;
    }

    private static void SetRootCurveValue(AnimationClip clip, string propertyName, float value, float clipLength)
    {
        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(Animator),
            propertyName = propertyName
        };

        AnimationCurve constantCurve = new AnimationCurve(
            new Keyframe(0f, value),
            new Keyframe(clipLength, value));

        AnimationUtility.SetEditorCurve(clip, binding, constantCurve);
    }

    private static GameObject FindSceneDoctor()
    {
        DoctorInteractable doctorInteractable = Object.FindObjectOfType<DoctorInteractable>(true);
        if (doctorInteractable != null)
        {
            return doctorInteractable.gameObject;
        }

        return GameObject.Find(SceneDoctorName);
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform nestedChild = FindChildRecursive(child, childName);
            if (nestedChild != null)
            {
                return nestedChild;
            }
        }

        return null;
    }

    private static bool Approximately(Vector3 left, Vector3 right)
    {
        return Vector3.SqrMagnitude(left - right) <= AlignmentTolerance * AlignmentTolerance;
    }

    private static bool ApproximatelyEuler(Vector3 left, Vector3 right)
    {
        return Mathf.Abs(Mathf.DeltaAngle(left.x, right.x)) <= 0.05f
            && Mathf.Abs(Mathf.DeltaAngle(left.y, right.y)) <= 0.05f
            && Mathf.Abs(Mathf.DeltaAngle(left.z, right.z)) <= 0.05f;
    }
}
