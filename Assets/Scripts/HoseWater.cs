using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HoseWater : MonoBehaviour
{
    public ParticleSystem waterParticle;
    public XRGrabInteractable grab;

    private void Start()
    {
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        StartWater();
    }

    void OnRelease(SelectExitEventArgs args)
    {
        StopWater();
    }

    void StartWater()
    {
        if (!waterParticle.isPlaying)
            waterParticle.Play();
    }

    void StopWater()
    {
        if (waterParticle.isPlaying)
            waterParticle.Stop();
    }
}