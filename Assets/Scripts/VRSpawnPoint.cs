using UnityEngine;
using Unity.XR.CoreUtils;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Oyun basladiginda XR Origin'i belirlenen spawn noktasina tasinir.
/// Bu scripti bos bir GameObject'e ekle ve pozisyonunu giriste istedigin yere koy.
/// XR Origin'i Inspector'dan surukle.
/// </summary>
[DefaultExecutionOrder(-10000)]
public class VRSpawnPoint : MonoBehaviour
{
    private static readonly string[] AutoMarkerNames = { "VR_SpawnPoint", "VRSpawnPoint", "PlayerSpawn" };
    private const int ExternalSpawnFallbackFrameCount = 8;
    private static bool _externalSpawnControlEnabled;
    private static bool _externalSpawnApplied;

    [Header("XR Origin'i buraya surukle")]
    [SerializeField] private XROrigin xrOrigin;

    [Header("Spawn Ayarlari")]
    [Tooltip("Spawn noktasi (bu objenin pozisyonu kullanilir)")]
    [SerializeField] private bool useThisObjectPosition = true;

    [Tooltip("Manuel pozisyon (useThisObjectPosition kapali ise)")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 0f, -15f);

    [Tooltip("Spawn yonu (Y ekseni rotasyonu)")]
    [SerializeField] private float spawnYRotation = 0f;

    [Header("Stabilizasyon")]
    [Tooltip("Ilk frame'lerde XR sisteminin offset uygulamasina karsi spawn'i tekrar uygular")]
    [SerializeField] private int reapplyFrameCount = 4;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetExternalSpawnControl()
    {
        _externalSpawnControlEnabled = false;
        _externalSpawnApplied = false;
    }

    public static void EnableExternalSpawnControlForNextScene()
    {
        _externalSpawnControlEnabled = true;
        _externalSpawnApplied = false;
    }

    public static void MarkExternalSpawnApplied()
    {
        _externalSpawnApplied = true;
        _externalSpawnControlEnabled = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawnFromNamedMarker()
    {
        // Sahnede en az bir VRSpawnPoint component varsa normal akis calissin.
        if (FindActiveSceneSpawnPoint() != null)
            return;

        // Dis spawn kontrolu aktifse (sahne gecisi), ApplyTransitionSpawnRoutine devralir.
        if (_externalSpawnControlEnabled)
            return;

        var xr = FindActiveSceneXROrigin();
        if (xr == null)
            return;

        Transform marker = FindNamedMarker();
        Vector3 spawnPos;
        float spawnYaw;
        if (marker != null)
        {
            spawnPos = marker.position;
            spawnYaw = marker.eulerAngles.y;
        }
        else
        {
            // Marker yoksa sahnedeki XR Rig pozunu fallback olarak kullan.
            spawnPos = xr.transform.position;
            spawnYaw = xr.transform.eulerAngles.y;
            Debug.Log("[VRSpawnPoint] Named marker bulunamadi, XR rig pozisyonu fallback olarak kullaniliyor.");
        }

        var runnerObj = new GameObject("__VRSpawnRuntimeRunner");
        var runner = runnerObj.AddComponent<RuntimeSpawnRunner>();
        runner.Begin(xr, spawnPos, spawnYaw, 4);
    }

    IEnumerator Start()
    {
        if (xrOrigin == null)
        {
            xrOrigin = FindActiveSceneXROrigin();
            if (xrOrigin == null)
            {
                Debug.LogError("[VRSpawnPoint] XR Origin bulunamadi!");
                yield break;
            }
        }

        if (_externalSpawnControlEnabled)
        {
            for (int i = 0; i < ExternalSpawnFallbackFrameCount; i++)
            {
                if (_externalSpawnApplied)
                {
                    yield break;
                }

                yield return null;
            }

            if (_externalSpawnApplied)
            {
                yield break;
            }

            _externalSpawnControlEnabled = false;
            Debug.LogWarning("[VRSpawnPoint] Dis spawn kontrolu zaman asimina ugradi. Yerel spawn fallback uygulaniyor.");
        }

        // CC aktifken CheckCapsule/OverlapCapsule kendi kolayderini overlap sayabilir ve
        // re-apply loop'ta kumulatif yukari drift yaratir. Re-apply suresi boyunca CC'yi kapatip
        // spawn bittikten sonra eski state'e geri aliyoruz.
        CharacterController initialCharacterController = xrOrigin.GetComponent<CharacterController>();
        bool restoreInitialCharacterController = initialCharacterController != null && initialCharacterController.enabled;
        if (initialCharacterController != null)
        {
            initialCharacterController.enabled = false;
        }

        try
        {
            // XR cihaz offset'i bazen Start anindan sonra guncellendigi icin,
            // spawn konumunu ilk birkac frame boyunca tekrar uygulariz.
            yield return null;
            ApplySpawn();

            for (int i = 0; i < reapplyFrameCount; i++)
            {
                yield return null;
                ApplySpawn();
            }
        }
        finally
        {
            if (initialCharacterController != null)
            {
                initialCharacterController.enabled = restoreInitialCharacterController;
            }
        }
    }

    private void ApplySpawn()
    {
        Vector3 targetPos = useThisObjectPosition ? transform.position : spawnPosition;
        ApplySpawnToOriginRigRoot(xrOrigin, targetPos, spawnYRotation);
    }

    private static Transform FindNamedMarker()
    {
        for (int i = 0; i < AutoMarkerNames.Length; i++)
        {
            Transform markerInScene = FindNamedTransformInActiveScene(AutoMarkerNames[i]);
            if (markerInScene != null)
            {
                return markerInScene;
            }
        }

        return null;
    }

    public static bool TryRespawnPlayerAtSceneSpawn(MonoBehaviour runner)
    {
        if (!TryResolveSceneAuthoredSpawnPose(out Vector3 targetPos, out float targetYaw, out int reapplyFrames))
            return false;

        return TryRespawnPlayerRigRoot(runner, targetPos, targetYaw, reapplyFrames);
    }

    public static bool TryResolveSceneAuthoredSpawnPose(out Vector3 targetPos, out float targetYaw, out int reapplyFrames)
    {
        targetPos = Vector3.zero;
        targetYaw = 0f;
        reapplyFrames = 0;

        VRSpawnPoint spawnPoint = FindActiveSceneSpawnPoint();
        if (spawnPoint != null)
        {
            targetPos = spawnPoint.useThisObjectPosition ? spawnPoint.transform.position : spawnPoint.spawnPosition;
            targetYaw = spawnPoint.spawnYRotation;
            reapplyFrames = Mathf.Max(0, spawnPoint.reapplyFrameCount);
            return true;
        }

        Transform marker = FindNamedMarker();
        if (marker != null)
        {
            targetPos = marker.position;
            targetYaw = marker.eulerAngles.y;
            reapplyFrames = 4;
            return true;
        }

        return false;
    }

    public static bool TryRespawnPlayer(MonoBehaviour runner, Vector3 targetPos, float targetYaw, int reapplyFrames = 4)
    {
        return TryRespawnPlayerRigRoot(runner, targetPos, targetYaw, reapplyFrames);
    }

    public static bool TryRespawnPlayerRigRoot(MonoBehaviour runner, Vector3 targetPos, float targetYaw, int reapplyFrames = 4)
    {
        return TryRespawnPlayerInternal(runner, targetPos, targetYaw, reapplyFrames);
    }

    public static bool TryRespawnPlayerRigRoot(
        MonoBehaviour runner,
        XROrigin origin,
        Vector3 targetPos,
        float targetYaw,
        int reapplyFrames = 4)
    {
        if (runner == null || origin == null)
            return false;

        runner.StartCoroutine(RespawnRoutine(origin, targetPos, targetYaw, Mathf.Max(0, reapplyFrames)));
        return true;
    }

    private static bool TryRespawnPlayerInternal(MonoBehaviour runner, Vector3 targetPos, float targetYaw, int reapplyFrames)
    {
        if (runner == null)
            return false;

        var xr = FindActiveSceneXROrigin();
        if (xr == null)
            return false;

        runner.StartCoroutine(RespawnRoutine(xr, targetPos, targetYaw, Mathf.Max(0, reapplyFrames)));
        return true;
    }

    private static bool TryResolveSpawnPose(out Vector3 targetPos, out float targetYaw, out int reapplyFrames)
    {
        if (TryResolveSceneAuthoredSpawnPose(out targetPos, out targetYaw, out reapplyFrames))
        {
            return true;
        }

        XROrigin rigFallback = FindActiveSceneXROrigin();
        if (rigFallback == null)
            return false;

        targetPos = rigFallback.transform.position;
        targetYaw = rigFallback.transform.eulerAngles.y;
        reapplyFrames = 4;
        Debug.Log("[VRSpawnPoint] Sahnede spawn marker bulunamadi, XR rig pozisyonuna fallback uygulaniyor.");
        return true;
    }

    public static Vector3 ResolveCameraTargetAboveGround(Vector3 targetPos)
    {
        if (!TryResolveGroundY(targetPos, out float groundY))
        {
            return targetPos;
        }

        const float GroundClearance = 0.08f;
        float minimumRigY = groundY + GroundClearance;
        if (targetPos.y < minimumRigY)
        {
            targetPos.y = minimumRigY;
        }

        return targetPos;
    }

    private static bool TryResolveGroundY(Vector3 targetPos, out float groundY)
    {
        const float MinGroundNormalY = 0.45f;
        const float NearProbeHeight = 0.6f;
        const float NearProbeDistance = 3f;

        Vector3 nearRayStart = targetPos + Vector3.up * NearProbeHeight;
        if (Physics.Raycast(nearRayStart, Vector3.down, out RaycastHit nearHit, NearProbeDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (nearHit.normal.y >= MinGroundNormalY)
            {
                groundY = nearHit.point.y;
                return true;
            }
        }

        const float WideProbeHeight = 4f;
        const float WideProbeDistance = 8f;
        // Authored rig pozu yanlislikla 2-3m derin kalmis olsa bile kurtarabilmek icin
        // raise penceresini genislettik. Stacked collider senaryosu "target Y'ye en yakin aday"
        // secimi ile korunur (asagida "nearest candidate" filtresi).
        const float MaxRaiseAboveTarget = 3.0f;
        const float MaxDropBelowTarget = 1.0f;

        RaycastHit[] hits = Physics.RaycastAll(
            targetPos + (Vector3.up * WideProbeHeight),
            Vector3.down,
            WideProbeDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        bool foundCandidate = false;
        float bestCandidateY = 0f;
        float bestAbsDelta = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.normal.y < MinGroundNormalY)
            {
                continue;
            }

            float deltaFromTarget = hit.point.y - targetPos.y;
            if (deltaFromTarget > MaxRaiseAboveTarget || deltaFromTarget < -MaxDropBelowTarget)
            {
                continue;
            }

            float absDelta = Mathf.Abs(deltaFromTarget);
            if (!foundCandidate || absDelta < bestAbsDelta)
            {
                bestCandidateY = hit.point.y;
                bestAbsDelta = absDelta;
                foundCandidate = true;
            }
        }

        if (foundCandidate)
        {
            groundY = bestCandidateY;
            return true;
        }

        groundY = 0f;
        return false;
    }

    private static void ApplySpawnToOriginRigRoot(XROrigin origin, Vector3 targetPos, float targetYRotation)
    {
        if (origin == null) return;

        // 1) Temel zemin kaldirma (CC yoksa ya da legacy davranis icin yeterli).
        targetPos = ResolveCameraTargetAboveGround(targetPos);

        // 2) CC feet'i zeminin uzerinde olsun. XROrigin.transform.position = rig root (genellikle feet ile
        //    ayni Y); ama CC.center.y != height/2 ise CC ayaklari origin Y'sinden (height/2 - center.y) kadar
        //    ASAGIDA olabilir. Bu durumda origin.y = ground + 0.08 dahi olsa feet = ground - X olur ve
        //    oyuncu zemine bater. Bunu CC feet offset'ini ekleyerek telafi ediyoruz.
        CharacterController ccForFeet = origin.GetComponent<CharacterController>();
        if (ccForFeet != null && TryResolveGroundY(targetPos, out float groundYForFeet))
        {
            float feetBelowOrigin = Mathf.Max(0f, (ccForFeet.height * 0.5f) - ccForFeet.center.y);
            const float FeetClearance = 0.05f;
            float minOriginY = groundYForFeet + feetBelowOrigin + FeetClearance;
            if (targetPos.y < minOriginY)
            {
                targetPos.y = minOriginY;
            }
        }

        // 3) Collider ici spawn kacisi (CC self-overlap'i filtrelenmis).
        targetPos = ResolveRigPositionOutsideColliders(origin, targetPos);

        origin.transform.rotation = Quaternion.Euler(0f, targetYRotation, 0f);
        origin.transform.position = targetPos;

        Transform cameraOffset = origin.CameraFloorOffsetObject?.transform;
        if (cameraOffset != null)
        {
            cameraOffset.localPosition = new Vector3(0f, cameraOffset.localPosition.y, 0f);
        }
    }

    private static readonly Collider[] s_overlapBuffer = new Collider[64];

    private static Vector3 ResolveRigPositionOutsideColliders(XROrigin origin, Vector3 targetPos)
    {
        CharacterController controller = origin.GetComponent<CharacterController>();
        if (controller == null)
        {
            return targetPos;
        }

        float radius = Mathf.Max(0.04f, controller.radius - controller.skinWidth);
        float height = Mathf.Max(controller.height, (radius * 2f) + 0.05f);
        float halfSegment = Mathf.Max(0f, (height * 0.5f) - radius);
        Vector3 centerOffset = controller.center;
        Transform rigRoot = origin.transform;
        Collider selfCollider = controller;

        const float stepHeight = 0.1f;
        const int maxSteps = 8;
        for (int i = 0; i <= maxSteps; i++)
        {
            Vector3 candidatePos = targetPos + (Vector3.up * (stepHeight * i));
            Vector3 worldCenter = candidatePos + centerOffset;
            Vector3 p1 = worldCenter + (Vector3.up * halfSegment);
            Vector3 p2 = worldCenter - (Vector3.up * halfSegment);

            int overlapCount = Physics.OverlapCapsuleNonAlloc(
                p1, p2, radius, s_overlapBuffer, ~0, QueryTriggerInteraction.Ignore);

            bool realOverlap = HasRealOverlap(s_overlapBuffer, overlapCount, selfCollider, rigRoot);
            if (!realOverlap && overlapCount >= s_overlapBuffer.Length)
            {
                Collider[] expandedHits = Physics.OverlapCapsule(p1, p2, radius, ~0, QueryTriggerInteraction.Ignore);
                realOverlap = HasRealOverlap(expandedHits, expandedHits.Length, selfCollider, rigRoot);
            }

            if (!realOverlap)
            {
                return candidatePos;
            }
        }

        return targetPos;
    }

    private static bool HasRealOverlap(Collider[] overlaps, int overlapCount, Collider selfCollider, Transform rigRoot)
    {
        for (int j = 0; j < overlapCount; j++)
        {
            Collider other = overlaps[j];
            if (other == null)
            {
                continue;
            }

            // Rig'in kendi CharacterController'ini veya alt hiyerarsideki herhangi bir collider'i
            // (hand colliders, tracked device colliders vb.) self-overlap olarak filtrele.
            if (other == selfCollider)
            {
                continue;
            }

            if (rigRoot != null && other.transform.IsChildOf(rigRoot))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static XROrigin FindActiveSceneXROrigin()
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

                XROrigin xrOrigin = roots[i].GetComponentInChildren<XROrigin>(true);
                if (xrOrigin != null)
                {
                    return xrOrigin;
                }
            }
        }

        return XRCameraHelper.GetXROrigin();
    }

    private static VRSpawnPoint FindActiveSceneSpawnPoint()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        VRSpawnPoint[] spawnPoints = FindObjectsOfType<VRSpawnPoint>(true);
        if (spawnPoints == null)
        {
            return null;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            VRSpawnPoint candidate = spawnPoints[i];
            if (candidate == null)
            {
                continue;
            }

            if (activeScene.IsValid() && candidate.gameObject.scene != activeScene)
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private static Transform FindNamedTransformInActiveScene(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindInChildrenByName(roots[i].transform, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindInChildrenByName(Transform parent, string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        if (string.Equals(parent.name, objectName, System.StringComparison.Ordinal))
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindInChildrenByName(parent.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static IEnumerator RespawnRoutine(XROrigin origin, Vector3 targetPos, float targetYaw, int reapplyFrames)
    {
        if (origin == null)
            yield break;

        CharacterController characterController = origin.GetComponent<CharacterController>();
        bool restoreCharacterController = characterController != null && characterController.enabled;
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        try
        {
            yield return null;
            ApplySpawnToOriginRigRoot(origin, targetPos, targetYaw);

            for (int i = 0; i < reapplyFrames; i++)
            {
                yield return null;
                ApplySpawnToOriginRigRoot(origin, targetPos, targetYaw);
            }
        }
        finally
        {
            if (characterController != null)
            {
                characterController.enabled = restoreCharacterController;
            }
        }
    }

    private sealed class RuntimeSpawnRunner : MonoBehaviour
    {
        private XROrigin xr;
        private Vector3 pos;
        private float yaw;
        private int reapplyCount;

        public void Begin(XROrigin xrOrigin, Vector3 targetPos, float targetYaw, int frames)
        {
            xr = xrOrigin;
            pos = targetPos;
            yaw = targetYaw;
            reapplyCount = Mathf.Max(0, frames);
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            CharacterController cc = xr != null ? xr.GetComponent<CharacterController>() : null;
            bool ccWasEnabled = cc != null && cc.enabled;
            if (cc != null)
            {
                cc.enabled = false;
            }

            try
            {
                yield return null;
                ApplySpawnToOriginRigRoot(xr, pos, yaw);

                for (int i = 0; i < reapplyCount; i++)
                {
                    yield return null;
                    ApplySpawnToOriginRigRoot(xr, pos, yaw);
                }
            }
            finally
            {
                if (cc != null)
                {
                    cc.enabled = ccWasEnabled;
                }

                Destroy(gameObject);
            }
        }
    }
}
