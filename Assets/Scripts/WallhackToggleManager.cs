using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class WallhackToggleManager : MonoBehaviour
{
    [Header("Toggle Ayarlari")]
    [Tooltip("Baslangicta wallhack aktif mi?")]
    public bool baslangictaAktif = false;

    private bool currentState;

    void Start()
    {
        currentState = baslangictaAktif;

        var tumu = FindObjectsOfType<YaraliWallhack>(true);
        foreach (var wh in tumu)
        {
            if (currentState)
                wh.EnableWallhack();
            else
                wh.DisableWallhack();
        }
    }

    void Update()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
            pressed = true;
#else
        if (Input.GetKeyDown(KeyCode.H))
            pressed = true;
#endif

        if (pressed)
        {
            currentState = !currentState;

            var tumu = FindObjectsOfType<YaraliWallhack>(true);
            foreach (var wh in tumu)
            {
                if (currentState)
                    wh.EnableWallhack();
                else
                    wh.DisableWallhack();
            }

            Debug.Log($"[WallhackToggle] Wallhack {(currentState ? "AKTIF" : "KAPALI")} - {tumu.Length} yarali etkilendi");
        }
    }
}
