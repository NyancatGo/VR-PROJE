using UnityEngine;

public class AxeBreak : MonoBehaviour
{
    public float breakForce = 2f;
    public new BoxCollider collider;
 
    private void OnCollisionEnter(Collision collision)
    {
        // hız kontrolü (VR hissi için)
        if (collision.relativeVelocity.magnitude < breakForce)
            return;

        if (collision.gameObject.CompareTag("Glass"))
        {
            BreakGlass(collision.gameObject);
            collider.enabled = false;
        }
    }

    void BreakGlass(GameObject glass)
    {
        Debug.Log("💥 Cam kırıldı!");

        // camı yok et
        Destroy(glass);
    }
}