using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ConeSnap : MonoBehaviour
{
    private XRGrabInteractable grab;
    private Rigidbody rb;

    private bool isPlaced = false;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPlaced) return;

        SnapPoint point = other.GetComponent<SnapPoint>();

        if (point != null && !point.isOccupied)
        {
            SnapToPoint(point);
        }
    }

    void SnapToPoint(SnapPoint point)
    {
        transform.position = point.transform.position;
        transform.rotation = point.transform.rotation;

        rb.isKinematic = true;
        grab.enabled = false;

        point.Occupy();

        isPlaced = true;

        // Göreve haber ver
        TaskManager.Instance.ConePlaced();
    }
}