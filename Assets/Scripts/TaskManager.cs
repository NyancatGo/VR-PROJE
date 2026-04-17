using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance;

    public int totalCones = 5;
    private int placedCones = 0;

    public TextMeshProUGUI missionText;
    public TextMeshProUGUI counterText;

    public int equipmentNeeded = 2; // kask + balta
    private int equippedCount = 0;
    private int fireCount = 0;
    private readonly HashSet<string> equippedItems = new HashSet<string>();

    public string AnaSahneAdi;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TaskManager] Duplicate instance detected, replacing previous reference.");
        }

        Instance = this;
    }

    public void ConePlaced()
    {
        placedCones++;

        Debug.Log("Yerlestirilen: " + placedCones);

        if (counterText != null)
        {
            counterText.SetText(placedCones + "/4");
        }

        if (placedCones >= totalCones)
        {
            CompleteTask();
        }
    }

    private void CompleteTask()
    {
        Debug.Log("Gorev Tamamlandi!");

        if (missionText != null)
        {
            missionText.SetText("Gorev Tamamlandi");
            missionText.fontSize = 70;
        }

        StartCoroutine(NextMission());
    }

    private IEnumerator NextMission()
    {
        yield return new WaitForSeconds(3);

        if (missionText != null)
        {
            missionText.fontSize = 40;
            missionText.SetText("2. GOREV: Itfaiye aracindaki ekipmanlari kusan (Balta, Kask)");
        }

        if (counterText != null)
        {
            counterText.SetText("0/2");
        }
    }

    public void OnEquipmentEquipped()
    {
        OnEquipmentEquipped(null);
    }

    public void OnEquipmentEquipped(string equipmentId)
    {
        if (!string.IsNullOrEmpty(equipmentId) && !equippedItems.Add(equipmentId))
        {
            Debug.Log("[TaskManager] Equipment already counted: " + equipmentId);
            return;
        }

        if (equippedCount >= equipmentNeeded)
        {
            Debug.Log("[TaskManager] Equipment goal already completed. Ignoring extra equip event.");
            return;
        }

        equippedCount++;

        if (counterText != null)
        {
            counterText.SetText(equippedCount + "/" + equipmentNeeded);
        }

        Debug.Log("[TaskManager] Equipment progress: " + equippedCount + "/" + equipmentNeeded + (string.IsNullOrEmpty(equipmentId) ? string.Empty : " (" + equipmentId + ")"));

        if (equippedCount >= equipmentNeeded)
        {
            Debug.Log("[TaskManager] Equipment task completed.");

            if (missionText != null)
            {
                missionText.SetText("Gorev Tamamlandi");
                missionText.fontSize = 70;
            }

            StartCoroutine(NextMission2());
        }
    }

    private IEnumerator NextMission2()
    {
        yield return new WaitForSeconds(3);

        if (missionText != null)
        {
            missionText.fontSize = 30;
            missionText.SetText("3. GOREV: Icerideki yaraliyi kurtar ve ambulansin yanina gotur. Evin kapisi disaridan kitli oldugu icin baltani kullanarak cami kir ve eve gir.");
        }

        if (counterText != null)
        {
            counterText.gameObject.SetActive(false);
        }
    }

    public void NPCCured()
    {
        Debug.Log("yarali gorevi tamamlandi!");

        if (missionText != null)
        {
            missionText.SetText("Gorev Tamamlandi");
            missionText.fontSize = 70;
        }

        StartCoroutine(NextMission3());
    }

    private IEnumerator NextMission3()
    {
        yield return new WaitForSeconds(3);

        if (missionText != null)
        {
            missionText.fontSize = 40;
            missionText.SetText("4. GOREV: Itfaiye aracindan yangin nozulunu al ve yangini sondur!.");
        }

        if (counterText != null)
        {
            counterText.gameObject.SetActive(true);
            counterText.SetText("%0");
        }
    }

    public void Firefire()
    {
        fireCount++;

        if (counterText != null)
        {
            counterText.SetText("%" + (fireCount * 10));
        }

        if (fireCount == 10 && missionText != null)
        {
            missionText.SetText("EGITIM TAMAMLANDI. TEBRIKLER :)");
        }
    }

    private IEnumerator Final()
    {
        yield return new WaitForSeconds(5);

        if (!string.IsNullOrEmpty(AnaSahneAdi))
        {
            XRSceneRuntimeStabilizer.PrepareForSceneTransition();
            XRCameraHelper.ClearCache();
            SceneManager.LoadScene(AnaSahneAdi);
        }
    }
}
