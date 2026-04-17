using UnityEngine;

public class HelmetEquip : MonoBehaviour
{
    public Transform headPoint;
    private bool equipped = false;

    private const string EquipmentId = "helmet";

    private void OnTriggerEnter(Collider other)
    {
        if (equipped)
        {
            return;
        }

        if (other.transform == headPoint)
        {
            Equip();
        }
    }

    private void Equip()
    {
        equipped = true;
        gameObject.SetActive(false);

        var taskManager = TaskManager.Instance;
        if (taskManager == null)
        {
            Debug.LogWarning("[HelmetEquip] TaskManager.Instance is null. Helmet equip could not be counted.");
            return;
        }

        taskManager.OnEquipmentEquipped(EquipmentId);
        Debug.Log("[HelmetEquip] Helmet equipped and reported to TaskManager.");
    }
}
