using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class VRUIClickHelper : MonoBehaviour
{
    [Header("Haptic")]
    [SerializeField] private bool enableHaptics = true;
    [SerializeField] private float hapticDuration = 0.04f;
    [SerializeField] [Range(0f, 1f)] private float hapticAmplitude = 0.12f;

    private bool rightGripWasPressed;
    private bool leftGripWasPressed;
    private bool rightTriggerWasPressed;
    private bool leftTriggerWasPressed;
    private bool rightPrimaryWasPressed;
    private bool leftPrimaryWasPressed;
    private bool keyboardFallbackBlocked;
    private Coroutine restoreInputFocusRoutine;
    private float suppressInputUntil;

    private static readonly List<Selectable> SelectableBuffer = new List<Selectable>(32);

    private void Update()
    {
        if (!gameObject.activeInHierarchy || Time.unscaledTime < suppressInputUntil)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        bool textInputFocused = false;
        if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
        {
            TMP_InputField selectedInputField = eventSystem.currentSelectedGameObject.GetComponentInParent<TMP_InputField>();
            textInputFocused = selectedInputField != null && selectedInputField.isFocused;
        }

        bool pressed = false;
        bool allowKeyboardSubmit = !keyboardFallbackBlocked || textInputFocused;
        bool keyboardSubmitPressed = false;
        bool mousePressedThisFrame = false;
        XRRayInteractor[] rayInteractors = FindObjectsOfType<XRRayInteractor>(true);

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            keyboardSubmitPressed = Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
        }

        if (!pressed && allowKeyboardSubmit && keyboardSubmitPressed)
        {
            pressed = true;
        }

        if (!pressed && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            mousePressedThisFrame = true;
            pressed = true;
        }
#else
        if (allowKeyboardSubmit && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            keyboardSubmitPressed = true;
            pressed = true;
        }

        if (!pressed && Input.GetMouseButtonDown(0))
        {
            mousePressedThisFrame = true;
            pressed = true;
        }
#endif

        bool rightRaySelectPressedThisFrame = WasInteractorSelectPressedThisFrame(rayInteractors, UnityEngine.XR.XRNode.RightHand);
        bool leftRaySelectPressedThisFrame = WasInteractorSelectPressedThisFrame(rayInteractors, UnityEngine.XR.XRNode.LeftHand);

        UnityEngine.XR.InputDevice rightHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
        bool rightGripNow = IsFeaturePressed(rightHand, UnityEngine.XR.CommonUsages.gripButton, UnityEngine.XR.CommonUsages.grip);
        bool rightTriggerNow = IsFeaturePressed(rightHand, UnityEngine.XR.CommonUsages.triggerButton, UnityEngine.XR.CommonUsages.trigger);
        bool rightPrimaryNow = rightHand.isValid &&
                       rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool rightPrimary) &&
                       rightPrimary;

        UnityEngine.XR.InputDevice leftHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
        bool leftGripNow = IsFeaturePressed(leftHand, UnityEngine.XR.CommonUsages.gripButton, UnityEngine.XR.CommonUsages.grip);
        bool leftTriggerNow = IsFeaturePressed(leftHand, UnityEngine.XR.CommonUsages.triggerButton, UnityEngine.XR.CommonUsages.trigger);
        bool leftPrimaryNow = leftHand.isValid &&
                      leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool leftPrimary) &&
                      leftPrimary;

        bool rightGripPressedThisFrame = rightGripNow && !rightGripWasPressed;
        bool leftGripPressedThisFrame = leftGripNow && !leftGripWasPressed;
        bool rightTriggerPressedThisFrame = rightTriggerNow && !rightTriggerWasPressed;
        bool leftTriggerPressedThisFrame = leftTriggerNow && !leftTriggerWasPressed;
        bool rightPrimaryPressedThisFrame = rightPrimaryNow && !rightPrimaryWasPressed;
        bool leftPrimaryPressedThisFrame = leftPrimaryNow && !leftPrimaryWasPressed;
        bool rightHandPressedThisFrame = rightGripPressedThisFrame || rightTriggerPressedThisFrame || rightPrimaryPressedThisFrame || rightRaySelectPressedThisFrame;
        bool leftHandPressedThisFrame = leftGripPressedThisFrame || leftTriggerPressedThisFrame || leftPrimaryPressedThisFrame || leftRaySelectPressedThisFrame;

        if (rightHandPressedThisFrame || leftHandPressedThisFrame)
        {
            pressed = true;
        }

        rightGripWasPressed = rightGripNow;
        leftGripWasPressed = leftGripNow;
        rightTriggerWasPressed = rightTriggerNow;
        leftTriggerWasPressed = leftTriggerNow;
        rightPrimaryWasPressed = rightPrimaryNow;
        leftPrimaryWasPressed = leftPrimaryNow;

        if (!pressed || eventSystem == null)
        {
            return;
        }

        UnityEngine.XR.XRNode? preferredHand = null;
        if (rightHandPressedThisFrame && !leftHandPressedThisFrame)
        {
            preferredHand = UnityEngine.XR.XRNode.RightHand;
        }
        else if (leftHandPressedThisFrame && !rightHandPressedThisFrame)
        {
            preferredHand = UnityEngine.XR.XRNode.LeftHand;
        }

        XRRayInteractor forcedSingleRay = null;
        if (!preferredHand.HasValue && (keyboardSubmitPressed || mousePressedThisFrame))
        {
            forcedSingleRay = ResolveBestFallbackRay(rayInteractors, eventSystem.currentSelectedGameObject);
            if (forcedSingleRay == null)
            {
                return;
    }
}

        if (!TryResolveBestUiHit(rayInteractors, preferredHand, forcedSingleRay, out XRRayInteractor bestRay, out RaycastResult bestUiResult))
        {
            return;
        }

        GameObject target = bestUiResult.gameObject;
        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            button = PointerEventData.InputButton.Left,
            pointerCurrentRaycast = bestUiResult,
            pointerPressRaycast = bestUiResult
        };

        Selectable selectable = target.GetComponentInParent<Selectable>();
        if (selectable != null && selectable.interactable)
        {
            selectable.Select();
        }

        ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.pointerUpHandler);
        GameObject clickHandledBy = ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.pointerClickHandler);

        TMP_InputField inputField = target.GetComponentInParent<TMP_InputField>();
        if (inputField != null)
        {
            eventSystem.SetSelectedGameObject(inputField.gameObject);
            inputField.Select();
            inputField.ActivateInputField();
            inputField.MoveTextEnd(false);
            TrySendHapticPulse(bestRay);
            return;
        }

        Button button = target.GetComponentInParent<Button>();
        if (button != null && button.interactable)
        {
            if (clickHandledBy == null)
            {
                Debug.Log($"VR Lazer '{button.name}' butonuna tıkladı!");
                button.onClick.Invoke();
            }

            VRKeyboardManager keyboardManager = button.GetComponentInParent<VRKeyboardManager>();
            if (keyboardManager != null && keyboardManager.IsKeyboardTarget(button.gameObject))
            {
                keyboardManager.NotifyKeyboardPointerInteraction();
            }

            if (keyboardSubmitPressed)
            {
                RemoveKeyboardSubmitCharacter(eventSystem);
            }

            TrySendHapticPulse(bestRay);
            return;
        }

        if (clickHandledBy != null || selectable != null)
        {
            TrySendHapticPulse(bestRay);
        }
    }

    private bool TryResolveBestUiHit(
        XRRayInteractor[] rayInteractors,
        UnityEngine.XR.XRNode? preferredHand,
        XRRayInteractor forcedSingleRay,
        out XRRayInteractor bestRay,
        out RaycastResult bestUiResult)
    {
        bestRay = null;
        bestUiResult = default;

        if (rayInteractors == null || rayInteractors.Length == 0)
        {
            return false;
        }

        float bestDistance = float.MaxValue;
        bool hasBest = false;

        for (int i = 0; i < rayInteractors.Length; i++)
        {
            XRRayInteractor ray = rayInteractors[i];
            if (!IsRayEligibleForUi(ray))
            {
                continue;
            }

            if (forcedSingleRay != null && ray != forcedSingleRay)
            {
                continue;
            }

            if (preferredHand.HasValue && !BelongsToHand(ray, preferredHand.Value))
            {
                continue;
            }

            RaycastResult uiResult;
            if (!TryGetCanvasRaycastResult(ray, out uiResult))
            {
                continue;
            }

            GameObject target = uiResult.gameObject;

            float distance = uiResult.distance;
            bool betterDistance = distance < bestDistance - 0.0005f;
            bool closeEnoughTie = Mathf.Abs(distance - bestDistance) <= 0.0005f;
            bool preferRightHandOnTie = !preferredHand.HasValue
                                         && closeEnoughTie
                                         && BelongsToHand(ray, UnityEngine.XR.XRNode.RightHand)
                                         && (bestRay == null || !BelongsToHand(bestRay, UnityEngine.XR.XRNode.RightHand));

            if (!hasBest || betterDistance || preferRightHandOnTie)
            {
                bestDistance = distance;
                bestRay = ray;
                bestUiResult = uiResult;
                hasBest = true;
            }
        }

        return hasBest;
    }

    private bool IsTargetOnThisCanvas(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        Transform targetTransform = target.transform;
        return targetTransform == transform || targetTransform.IsChildOf(transform);
    }

    private static bool IsRayEligibleForUi(XRRayInteractor rayInteractor)
    {
        return rayInteractor != null
               && rayInteractor.isActiveAndEnabled
               && rayInteractor.gameObject.activeInHierarchy
               && rayInteractor.enableUIInteraction;
    }

    private bool TryGetCanvasRaycastResult(XRRayInteractor rayInteractor, out RaycastResult result)
    {
        result = default;

        if (rayInteractor == null)
        {
            return false;
        }

        if (rayInteractor.TryGetCurrentUIRaycastResult(out RaycastResult uiResult)
            && uiResult.gameObject != null
            && IsTargetOnThisCanvas(uiResult.gameObject))
        {
            result = uiResult;
            return true;
        }

        if (!TryResolveCanvasSelectableRayHit(rayInteractor, out GameObject fallbackTarget, out float fallbackDistance, out Vector3 worldPoint))
        {
            return false;
        }

        result = new RaycastResult
        {
            gameObject = fallbackTarget,
            distance = fallbackDistance,
            worldPosition = worldPoint,
            worldNormal = -GetRayDirection(rayInteractor),
            screenPosition = Vector2.zero
        };
        return true;
    }

    private bool TryResolveCanvasSelectableRayHit(
        XRRayInteractor rayInteractor,
        out GameObject target,
        out float distance,
        out Vector3 worldPoint)
    {
        target = null;
        distance = float.MaxValue;
        worldPoint = Vector3.zero;

        Transform rayOrigin = GetRayOrigin(rayInteractor);
        if (rayOrigin == null)
        {
            return false;
        }

        Vector3 rayDirection = rayOrigin.forward;
        if (rayDirection.sqrMagnitude <= 0.000001f)
        {
            return false;
        }

        rayDirection.Normalize();
        Ray ray = new Ray(rayOrigin.position, rayDirection);
        float maxDistance = Mathf.Max(0.01f, rayInteractor.maxRaycastDistance);

        SelectableBuffer.Clear();
        GetComponentsInChildren(true, SelectableBuffer);

        bool found = false;
        for (int i = 0; i < SelectableBuffer.Count; i++)
        {
            Selectable selectable = SelectableBuffer[i];
            if (!IsSelectableCandidate(selectable))
            {
                continue;
            }

            RectTransform rectTransform = selectable.transform as RectTransform;
            if (rectTransform == null)
            {
                continue;
            }

            Plane plane = new Plane(rectTransform.forward, rectTransform.position);
            if (!plane.Raycast(ray, out float hitDistance))
            {
                continue;
            }

            if (hitDistance < 0f || hitDistance > maxDistance)
            {
                continue;
            }

            Vector3 candidatePoint = ray.GetPoint(hitDistance);
            if (!IsPointInsideRect(rectTransform, candidatePoint))
            {
                continue;
            }

            if (!found || hitDistance < distance)
            {
                found = true;
                target = selectable.gameObject;
                distance = hitDistance;
                worldPoint = candidatePoint;
            }
        }

        SelectableBuffer.Clear();
        return found;
    }

    private static Transform GetRayOrigin(XRRayInteractor rayInteractor)
    {
        if (rayInteractor == null)
        {
            return null;
        }

        if (rayInteractor.rayOriginTransform != null)
        {
            return rayInteractor.rayOriginTransform;
        }

        if (rayInteractor.attachTransform != null)
        {
            return rayInteractor.attachTransform;
        }

        return rayInteractor.transform;
    }

    private static Vector3 GetRayDirection(XRRayInteractor rayInteractor)
    {
        Transform rayOrigin = GetRayOrigin(rayInteractor);
        return rayOrigin != null ? rayOrigin.forward : Vector3.forward;
    }

    private static bool IsSelectableCandidate(Selectable selectable)
    {
        if (selectable == null
            || !selectable.gameObject.activeInHierarchy
            || !selectable.isActiveAndEnabled
            || !selectable.IsInteractable())
        {
            return false;
        }

        Graphic targetGraphic = selectable.targetGraphic;
        if (targetGraphic != null && !targetGraphic.raycastTarget)
        {
            return false;
        }

        CanvasGroup[] groups = selectable.GetComponentsInParent<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            CanvasGroup group = groups[i];
            if (group == null || !group.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (group.alpha <= 0.001f || !group.blocksRaycasts)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPointInsideRect(RectTransform rectTransform, Vector3 worldPoint)
    {
        Vector3 localPoint3 = rectTransform.InverseTransformPoint(worldPoint);
        Vector2 localPoint = new Vector2(localPoint3.x, localPoint3.y);
        return rectTransform.rect.Contains(localPoint);
    }

    private static bool IsFeaturePressed(
        UnityEngine.XR.InputDevice device,
        UnityEngine.XR.InputFeatureUsage<bool> buttonFeature,
        UnityEngine.XR.InputFeatureUsage<float> analogFeature)
    {
        if (!device.isValid)
        {
            return false;
        }

        if (device.TryGetFeatureValue(buttonFeature, out bool buttonValue) && buttonValue)
        {
            return true;
        }

        return device.TryGetFeatureValue(analogFeature, out float analogValue) && analogValue > 0.65f;
    }

    private static bool WasInteractorSelectPressedThisFrame(XRRayInteractor[] rayInteractors, UnityEngine.XR.XRNode handNode)
    {
        if (rayInteractors == null || rayInteractors.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < rayInteractors.Length; i++)
        {
            XRRayInteractor rayInteractor = rayInteractors[i];
            if (!IsRayEligibleForUi(rayInteractor) || !BelongsToHand(rayInteractor, handNode))
            {
                continue;
            }

            XRBaseController controller = rayInteractor.xrController;
            if (controller != null && controller.selectInteractionState.activatedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private static bool BelongsToHand(XRRayInteractor rayInteractor, UnityEngine.XR.XRNode handNode)
    {
        if (rayInteractor == null)
        {
            return false;
        }

        XRBaseController controller = rayInteractor.xrController;
        if (controller != null)
        {
            XRController nodeBasedController = controller as XRController;
            if (nodeBasedController != null)
            {
                UnityEngine.XR.XRNode controllerNode = nodeBasedController.controllerNode;
                if (controllerNode == handNode)
                {
                    return true;
                }

                if (controllerNode == UnityEngine.XR.XRNode.LeftHand || controllerNode == UnityEngine.XR.XRNode.RightHand)
                {
                    return false;
                }
            }
        }

        string lowerName = rayInteractor.name != null ? rayInteractor.name.ToLowerInvariant() : string.Empty;
        string parentLowerName = rayInteractor.transform.parent != null
            ? rayInteractor.transform.parent.name.ToLowerInvariant()
            : string.Empty;
        string fullName = lowerName + " " + parentLowerName;
        if (handNode == UnityEngine.XR.XRNode.LeftHand)
        {
            return fullName.Contains("left") || fullName.Contains("sol");
        }

        if (handNode == UnityEngine.XR.XRNode.RightHand)
        {
            return fullName.Contains("right") || fullName.Contains("sag");
        }

        return true;
    }

    private XRRayInteractor ResolveBestFallbackRay(XRRayInteractor[] rayInteractors, GameObject currentlySelectedObject)
    {
        if (rayInteractors == null || rayInteractors.Length == 0)
        {
            return null;
        }

        XRRayInteractor bestRay = null;
        XRRayInteractor bestRightRay = null;
        float bestDistance = float.MaxValue;
        float bestRightDistance = float.MaxValue;
        XRRayInteractor selectedObjectRay = null;

        for (int i = 0; i < rayInteractors.Length; i++)
        {
            XRRayInteractor ray = rayInteractors[i];
            if (!IsRayEligibleForUi(ray) || !ray.TryGetCurrentUIRaycastResult(out RaycastResult tempResult))
            {
                continue;
            }

            GameObject hitObject = tempResult.gameObject;
            if (hitObject == null || !IsTargetOnThisCanvas(hitObject))
            {
                continue;
            }

            if (currentlySelectedObject != null)
            {
                Transform hitTransform = hitObject.transform;
                Transform selectedTransform = currentlySelectedObject.transform;
                if (hitTransform == selectedTransform || hitTransform.IsChildOf(selectedTransform) || selectedTransform.IsChildOf(hitTransform))
                {
                    selectedObjectRay = ray;
                }
            }

            if (tempResult.distance < bestDistance)
            {
                bestDistance = tempResult.distance;
                bestRay = ray;
            }

            if (BelongsToHand(ray, UnityEngine.XR.XRNode.RightHand))
            {
                if (bestRightRay == null || tempResult.distance < bestRightDistance)
                {
                    bestRightRay = ray;
                    bestRightDistance = tempResult.distance;
                }
            }
        }

        if (selectedObjectRay != null)
        {
            return selectedObjectRay;
        }

        return bestRightRay != null ? bestRightRay : bestRay;
    }

    public void SetKeyboardFallbackBlocked(bool blocked)
    {
        keyboardFallbackBlocked = blocked;
    }

    private void TrySendHapticPulse(XRRayInteractor rayInteractor)
    {
        if (!enableHaptics || rayInteractor == null)
        {
            return;
        }

        rayInteractor.SendHapticImpulse(Mathf.Clamp01(hapticAmplitude), Mathf.Max(0f, hapticDuration));
    }

    private void RemoveKeyboardSubmitCharacter(EventSystem eventSystem)
    {
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
        {
            return;
        }

        TMP_InputField selectedInputField = eventSystem.currentSelectedGameObject.GetComponentInParent<TMP_InputField>();
        if (selectedInputField == null || string.IsNullOrEmpty(selectedInputField.text))
        {
            return;
        }

        string text = selectedInputField.text;
        int trimmedLength = text.Length;
        while (trimmedLength > 0)
        {
            char tail = text[trimmedLength - 1];
            if (tail != '\n' && tail != '\r')
            {
                break;
            }

            trimmedLength--;
        }

        if (trimmedLength == text.Length)
        {
            return;
        }

        eventSystem.SetSelectedGameObject(null);
        if (selectedInputField.isFocused)
        {
            selectedInputField.DeactivateInputField(true);
        }
        else
        {
            selectedInputField.ReleaseSelection();
        }

        selectedInputField.text = text.Substring(0, trimmedLength);
        selectedInputField.ForceLabelUpdate();
        Canvas.ForceUpdateCanvases();
    }
}
