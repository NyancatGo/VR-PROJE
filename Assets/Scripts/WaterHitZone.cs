using UnityEngine;

public class WaterHitZone : MonoBehaviour
{
    public HoseWater hose;
    public ParticleSystem waterParticle;
    private void OnTriggerStay(Collider other)
    {

        if (!waterParticle.isPlaying) return; // 🔥 KRİTİK KONTROL

        if (other.CompareTag("Fire"))
        {
            Debug.Log("Sondurma info gonderildi");
            other.GetComponent<Fire>()?.StartExtinguish();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Fire"))
        {
            other.GetComponent<Fire>()?.StopExtinguish();
        }
    }
}