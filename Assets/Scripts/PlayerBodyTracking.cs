using UnityEngine;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(CapsuleCollider))]
public class PlayerBodyTracking : MonoBehaviour
{
    private CapsuleCollider col;
    private XROrigin xrOrigin;

    void Start()
    {
        col = GetComponent<CapsuleCollider>();
        xrOrigin = GetComponent<XROrigin>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void Update()
    {
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            // XR Origin'in kamerasinin (kafanin) havada durdugu pozisyona gore vucut yuksekligini belirler.
            Vector3 camLocalPos = xrOrigin.Camera.transform.localPosition;
            
            // Kapsul boyunu gozlugun yerden yuksekligi kadar yap.
            col.height = camLocalPos.y;
            
            // Merkezini ayak ucu ile kafa arasinin tam ortasina, x/z duzleminde de kafanin altina hizala.
            col.center = new Vector3(camLocalPos.x, camLocalPos.y / 2f, camLocalPos.z);
        }
    }
}