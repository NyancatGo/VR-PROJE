using UnityEngine;

public class SnapPoint : MonoBehaviour
{
    public bool isOccupied = false;
    private MeshRenderer meshRenderer;
    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }
    public void Occupy()
    {
        isOccupied = true;
        meshRenderer.enabled = false;
    }
}