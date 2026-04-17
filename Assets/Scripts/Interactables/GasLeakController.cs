using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
// using UnityEngine.XR.Content.Interaction; // Temporary removal due to compile error

namespace AfetSimulasyon.Modul2
{
    /// <summary>
    /// Vana objesine eklenir. XR Knob kullanılarak vananın çevrilmesiyle gaz kaçağını (ses ve partikül efekti) durdurur.
    /// </summary>
    public class GasLeakController : MonoBehaviour
    {
        [Header("Gas Visuals & Audio")]
        [Tooltip("The particle system for the gas leak effect.")]
        public ParticleSystem gasParticleSystem;

        [Tooltip("Audio source playing the hissing gas sound.")]
        public AudioSource gasAudioSource;

        [Header("XR Interaction")]
        [Tooltip("XR Knob component attached to the valve.")]
        // public XRKnob valveKnob;
        public Component valveKnob; // Temporarily changed to Component to avoid errors

        [Tooltip("Value (0 to 1) at which the valve is considered fully closed.")]
        [Range(0f, 1f)]
        public float closeThreshold = 1f;

        private bool isGasStopped = false;

        void OnEnable()
        {
            /*
            if (valveKnob != null)
            {
                valveKnob.onValueChange.AddListener(OnValveTurned);
            }
            else
            {
                Debug.LogWarning("GasLeakController: XRKnob component is not assigned on " + gameObject.name);
            }
            */
        }

        void OnDisable()
        {
            /*
            if (valveKnob != null)
            {
                valveKnob.onValueChange.RemoveListener(OnValveTurned);
            }
            */
        }

        private void OnValveTurned(float value)
        {
            if (isGasStopped) return;

            // Threshold kontrolü: Eğer vana belirlenen eşiğe kadar döndürüldüyse (varsayılan 1) gazı kes.
            if (value >= closeThreshold)
            {
                StopGasLeak();
            }
        }

        private void StopGasLeak()
        {
            isGasStopped = true;

            // Partikül efektini durdur (Stop emitting)
            if (gasParticleSystem != null && gasParticleSystem.isPlaying)
            {
                gasParticleSystem.Stop();
            }

            // Ses efektini durdur
            if (gasAudioSource != null && gasAudioSource.isPlaying)
            {
                // İsteğe bağlı olarak yavaşça sesi kısmak için coroutine kullanılabilir.
                gasAudioSource.Stop();
            }

            Debug.Log("<color=green>Gas Leak Stopped!</color> Value threshold reached.");
            
            // TODO: GameManager'a entegrasyon geldiğinde "Görev Tamamlandı" verisini yolla.
            // Example: GameManager.Instance.CompleteTask("GasLeak");
        }
    }
}
