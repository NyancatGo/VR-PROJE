using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

namespace TriyajModul3
{
    public class OnayMenusuManager : MonoBehaviour
    {
        [Header("Hedef Işınlanma Noktası")]
        [Tooltip("XROrigin'in ışınlanacağı hedef Transform")]
        public Transform hedefNokta;

        [Header("XR Referansları")]
        [Tooltip("Manuel olarak XROrigin atanabilir (opsiyonel)")]
        public XROrigin xrOrigin;

        [Header("Spawn Offset (Metre)")]
        [Tooltip("Spawn noktasına eklenecek offset. Eksen değerlerini hastane içine girecek şekilde ayarla.")]
        public Vector3 spawnOffset = Vector3.zero;

        [Header("Rotation Offset (Derece)")]
        [Tooltip("Y ekseninde eklenecek rotation offset. Kamera ters bakıyorsa 180 veya 90 ekle.")]
        public float spawnRotationOffset;

        [Header("Hız Ayarları")]
        [Tooltip("Teleport olduktan sonra karakterin yürüyüş hızı.")]
        public float hastaneHizi = 4f;

        private CharacterController characterController;

        [Header("Fade Efekti")]
        [Tooltip("Fade efekti kullanılsın mı?")]
        public bool useFadeEffect = true;
        [SerializeField] private int respawnReapplyFrameCount = 4;
        [SerializeField] private float initialConfirmBlockSeconds = 0.35f;

        private float menuShownAtUnscaledTime = float.NegativeInfinity;
        private bool waitingInitialInputRelease;

        private void Awake()
        {
            if (xrOrigin == null)
            {
                xrOrigin = XRCameraHelper.GetXROrigin();
            }
            if (xrOrigin != null)
            {
                characterController = xrOrigin.GetComponent<CharacterController>();
            }
        }

        private void OnEnable()
        {
            menuShownAtUnscaledTime = Time.unscaledTime;
            waitingInitialInputRelease = true;
            isTeleporting = false;
        }

        public void EvetTiklandi()
        {
            if (!CanAcceptConfirmationClick())
            {
                return;
            }

            if (isTeleporting)
            {
                Debug.LogWarning("[Teleport] Zaten teleport ediliyor, tekrar tıklama yok sayıldı.");
                return;
            }

            StartCoroutine(IsinlanVeGizle());
        }

        private bool CanAcceptConfirmationClick()
        {
            if (Time.unscaledTime - menuShownAtUnscaledTime < Mathf.Max(0f, initialConfirmBlockSeconds))
            {
                return false;
            }

            if (waitingInitialInputRelease)
            {
                if (IsAnyConfirmInputHeld())
                {
                    return false;
                }

                waitingInitialInputRelease = false;
            }

            return true;
        }

        private static bool IsAnyConfirmInputHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null
                && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
            {
                return true;
            }

            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.enterKey.isPressed
                    || UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed)
                {
                    return true;
                }
            }
#endif

            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand,
                devices);

            for (int i = 0; i < devices.Count; i++)
            {
                InputDevice device = devices[i];
                if (!device.isValid)
                {
                    continue;
                }

                if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButton) && triggerButton)
                {
                    return true;
                }

                if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButton) && gripButton)
                {
                    return true;
                }

                if (device.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue) && triggerValue > 0.65f)
                {
                    return true;
                }

                if (device.TryGetFeatureValue(CommonUsages.grip, out float gripValue) && gripValue > 0.65f)
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator IsinlanVeGizle()
        {
            isTeleporting = true;

            if (hedefNokta == null)
            {
                Debug.LogError("[Teleport] Hedef nokta atanmadı.");
                isTeleporting = false;
                yield break;
            }

            if (xrOrigin == null)
            {
                xrOrigin = XRCameraHelper.GetXROrigin();
            }

            if (xrOrigin == null)
            {
                Debug.LogError("[Teleport] XROrigin bulunamadı.");
                isTeleporting = false;
                yield break;
            }

            if (characterController == null)
            {
                characterController = xrOrigin.GetComponent<CharacterController>();
            }

            // Hoist locomotion refs before try so finally can access them
            LocomotionSystem locomotionSystem = xrOrigin.GetComponentInChildren<LocomotionSystem>(true);
            bool restoreLocomotion = locomotionSystem != null && locomotionSystem.enabled;

            try
            {
                if (useFadeEffect && FadeEffectManager.Instance != null)
                {
                    yield return FadeEffectManager.Instance.StartFadeToBlackCoroutine();
                }

                if (locomotionSystem != null)
                {
                    locomotionSystem.enabled = false;
                }

                if (characterController != null)
                {
                    characterController.enabled = false;
                }

                yield return null;
                yield return null;
                yield return null;

                bool teleportStarted = false;
                HospitalTriageManager hospitalManager = HospitalTriageManager.Instance;
                if (hospitalManager != null)
                {
                    teleportStarted = hospitalManager.RespawnPlayerAtHospitalStart();
                }

                if (!teleportStarted)
                {
                    Vector3 hedefPozisyon = ResolveSafeSpawnPosition(hedefNokta.position + spawnOffset);
                    float hedefYaw = hedefNokta.eulerAngles.y + spawnRotationOffset;
                    teleportStarted = VRSpawnPoint.TryRespawnPlayerRigRoot(this, hedefPozisyon, hedefYaw, respawnReapplyFrameCount);
                }

                if (!teleportStarted)
                {
                    Vector3 fallbackPozisyon = ResolveSafeSpawnPosition(hedefNokta.position + spawnOffset);
                    xrOrigin.transform.position = fallbackPozisyon;
                    float fallbackYaw = hedefNokta.eulerAngles.y + spawnRotationOffset;
                    xrOrigin.transform.rotation = Quaternion.Euler(0f, fallbackYaw, 0f);
                }

                ContinuousMoveProviderBase continuousMove = xrOrigin.GetComponentInChildren<ContinuousMoveProviderBase>(true);
                if (continuousMove != null)
                {
                    continuousMove.moveSpeed = hastaneHizi;
                }

                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;

                if (characterController != null)
                {
                    characterController.enabled = true;
                }

                if (locomotionSystem != null)
                {
                    locomotionSystem.enabled = restoreLocomotion;
                }

                HospitalTriageManager.Instance?.EnterHospitalPhase();

                // FadeFromBlackRoutine already waits stayBlackDuration internally — no double wait here
                if (useFadeEffect && FadeEffectManager.Instance != null)
                {
                    yield return FadeEffectManager.Instance.StartFadeFromBlackCoroutine();
                }
            }
            finally
            {
                isTeleporting = false;
                // Fail-safe: always restore locomotion and clear overlay on any exit path
                if (characterController != null)
                    characterController.enabled = true;
                if (locomotionSystem != null)
                    locomotionSystem.enabled = restoreLocomotion;
                if (FadeEffectManager.Instance != null)
                    FadeEffectManager.Instance.InstantFadeClear();
            }

            gameObject.SetActive(false);
        }

        private static Vector3 ResolveSafeSpawnPosition(Vector3 targetPosition)
        {
            return VRSpawnPoint.ResolveCameraTargetAboveGround(targetPosition);
        }
        private bool isTeleporting;

        public void HayirTiklandi()
        {
            if (!CanAcceptConfirmationClick())
            {
                return;
            }

            gameObject.SetActive(false);
        }
    }
}
