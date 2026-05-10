using UnityEngine;

public class Fire : MonoBehaviour
{
    public float health = 100f;
    public float reduceSpeed = 40f;

    private bool isBeingHit = false;

    void Update()
    {
        if (isBeingHit)
        {
            health -= reduceSpeed * Time.deltaTime;

            if (health <= 0)
            {
                Extinguish();
                var taskManager = TaskManager.Instance;
                if (taskManager == null)
                {
                    Debug.LogWarning("[Fire] TaskManager.Instance is null. Fire extinguish could not be counted.");
                    return;
                }

                taskManager.Firefire();
            }
        }
    }

    public void StartExtinguish()
    {
        isBeingHit = true;
    }

    public void StopExtinguish()
    {
        isBeingHit = false;
    }

    void Extinguish()
    {
        Debug.Log("🔥 Ateş söndü!");       
        Destroy(gameObject);
    }
}
