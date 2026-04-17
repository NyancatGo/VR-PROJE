using UnityEngine;

public class XRRigPositioner : MonoBehaviour
{
    [Header("Modul 1 (Sınıf) Ayarları")]
    public Vector3 modul1SeatPosition = new Vector3(68.8f, 0f, 23.4f);
    public float modul1RotationY = 180f;

    void Start()
    {
        // Şu anki sahnenin ismini kontrol et
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (sceneName == "Modul1")
        {
            ApplyPosition(modul1SeatPosition, modul1RotationY);
            Debug.Log($"XR Rig '{sceneName}' sahnesine göre konumlandırıldı: {modul1SeatPosition}");
        }
    }

    public void ApplyPosition(Vector3 pos, float rotY)
    {
        // XR Rig'i belirlediğimiz sıraya ışınla
        transform.position = pos;
        transform.rotation = Quaternion.Euler(0, rotY, 0);
    }
}
