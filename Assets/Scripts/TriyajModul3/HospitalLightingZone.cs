using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Triyaj hastanesine ozel lokal karanlik bir volume kurar.
/// Global volume'u degistirmez; yalnizca bu alanin icine giren kamerayi etkiler.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class HospitalLightingZone : MonoBehaviour
{
    private const string VolumeObjectName = "Hospital_Dark_Volume";
    private const float MinimumAxisSize = 1f;

    [Header("Dark Hospital Volume")]
    [Tooltip("Sadece bu hastane alaninda kullanilacak lokal VolumeProfile.")]
    [SerializeField] private VolumeProfile localDarkProfile;

    [Tooltip("Render bounds'a eklenecek ekstra lokal padding.")]
    [SerializeField] private Vector3 boundsPadding = new Vector3(8f, 4f, 8f);

    [Tooltip("Volume sinirindan disari cikarken gecisin ne kadar yumusak olacagi.")]
    [SerializeField] private float blendDistance = 2.5f;

    [Tooltip("Global volume'un ustune cikabilmesi icin lokal priority degeri.")]
    [SerializeField] private float priority = 20f;

    [Tooltip("Play mode'da lokal karanlik volume acik kalsin mi? Varsayilan kapali (normal, aydinlik atmosfer).")]
    [SerializeField] private bool enableDarkAtmosphereInPlayMode = false;

    private Transform volumeRoot;

    private void Reset()
    {
        if (!Application.isPlaying) SyncDarkVolume();
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            ApplyRuntimeVolumeState();
            return;
        }

        SyncDarkVolume();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) SyncDarkVolume();
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        // Transition kaynakli gecikmeli kurulumlara karsi bir kez daha uygula.
        ApplyRuntimeVolumeState();
    }

    [ContextMenu("Sync Hospital Dark Volume")]
    public void SyncDarkVolume()
    {
        if (!gameObject.scene.IsValid())
        {
            return;
        }

        if (localDarkProfile == null)
        {
            Debug.LogWarning("HospitalLightingZone: Local dark profile atanamadi.", this);
            return;
        }

        if (!TryCalculateLocalBounds(out Bounds localBounds))
        {
            Debug.LogWarning("HospitalLightingZone: Hastane render bounds hesaplaniyor ama hic renderer bulunamadi.", this);
            return;
        }

        localBounds.Expand(boundsPadding);

        Transform darkVolumeRoot = GetOrCreateVolumeRoot();
        darkVolumeRoot.gameObject.layer = gameObject.layer;
        darkVolumeRoot.localPosition = localBounds.center;
        darkVolumeRoot.localRotation = Quaternion.identity;
        darkVolumeRoot.localScale = Vector3.one;

        Vector3 localSize = localBounds.size;
        localSize.x = Mathf.Max(localSize.x, MinimumAxisSize);
        localSize.y = Mathf.Max(localSize.y, MinimumAxisSize);
        localSize.z = Mathf.Max(localSize.z, MinimumAxisSize);

        Volume volume = EnsureComponent<Volume>(darkVolumeRoot.gameObject);
        volume.enabled = enabled;
        volume.isGlobal = false;
        volume.priority = priority;
        volume.weight = 1f;
        volume.blendDistance = Mathf.Max(0f, blendDistance);
        volume.sharedProfile = localDarkProfile;

        BoxCollider boxCollider = EnsureComponent<BoxCollider>(darkVolumeRoot.gameObject);
        boxCollider.isTrigger = true;
        boxCollider.center = Vector3.zero;
        boxCollider.size = localSize;

        ApplyRuntimeVolumeState();
    }

    private void ApplyRuntimeVolumeState()
    {
        Transform existingRoot = transform.Find(VolumeObjectName);
        if (existingRoot == null)
        {
            return;
        }

        if (existingRoot.TryGetComponent(out Volume volume))
        {
            volume.enabled = enableDarkAtmosphereInPlayMode;
            volume.weight = enableDarkAtmosphereInPlayMode ? 1f : 0f;
        }

        if (existingRoot.TryGetComponent(out BoxCollider collider))
        {
            collider.enabled = enableDarkAtmosphereInPlayMode;
        }

        existingRoot.gameObject.SetActive(enableDarkAtmosphereInPlayMode);
    }

    private Transform GetOrCreateVolumeRoot()
    {
        if (volumeRoot != null)
        {
            return volumeRoot;
        }

        Transform existingRoot = transform.Find(VolumeObjectName);
        if (existingRoot != null)
        {
            volumeRoot = existingRoot;
            return volumeRoot;
        }

        GameObject darkVolumeObject = new GameObject(VolumeObjectName);
        darkVolumeObject.transform.SetParent(transform, false);
        volumeRoot = darkVolumeObject.transform;
        return volumeRoot;
    }

    private bool TryCalculateLocalBounds(out Bounds localBounds)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        localBounds = default;

        foreach (Renderer renderer in renderers)
        {
            if (volumeRoot != null && renderer.transform.IsChildOf(volumeRoot))
            {
                continue;
            }

            Bounds rendererBounds = renderer.bounds;
            if (rendererBounds.size == Vector3.zero)
            {
                continue;
            }

            EncapsulateWorldBounds(rendererBounds, ref localBounds, ref hasBounds);
        }

        return hasBounds;
    }

    private void EncapsulateWorldBounds(Bounds worldBounds, ref Bounds localBounds, ref bool hasBounds)
    {
        Vector3 min = worldBounds.min;
        Vector3 max = worldBounds.max;

        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 localCorner = transform.InverseTransformPoint(corners[i]);
            if (!hasBounds)
            {
                localBounds = new Bounds(localCorner, Vector3.zero);
                hasBounds = true;
                continue;
            }

            localBounds.Encapsulate(localCorner);
        }
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
    }
}
