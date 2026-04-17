using UnityEngine;
using UnityEngine.AI;

public class InjuredNPC : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Follow Settings")]
    public float followDistance   = 8f;
    public float stoppingDistance = 1.5f;

    [Header("NavMesh Recovery")]
    [Tooltip("NavMesh'e snap için max arama yarıçapı (metre)")]
    public float navMeshSampleRadius = 3f;
    [Tooltip("SetDestination çağrısı arasındaki minimum süre (sn)")]
    public float destinationUpdateInterval = 0.2f;

    // ── Private state ──────────────────────────────────────────────
    private NavMeshAgent agent;
    private Animator     animator;
    private bool         isFollowing  = false;
    private bool         isCompleted  = false;
    private float        nextUpdateTime = 0f;

    // ──────────────────────────────────────────────────────────────
    void Start()
    {
        agent    = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent == null)
        {
            Debug.LogError($"[InjuredNPC] '{name}' üzerinde NavMeshAgent bulunamadı!", this);
            enabled = false;
            return;
        }

        agent.stoppingDistance = stoppingDistance;

        // Spawn pozisyonu NavMesh dışındaysa en yakın noktaya ışınla
        TrySnapToNavMesh();
    }

    // ──────────────────────────────────────────────────────────────
    void Update()
    {
        if (isCompleted) return;
        if (player == null)  return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Takip başlat
        if (!isFollowing && distance < followDistance)
        {
            isFollowing = true;
            Debug.Log("[InjuredNPC] NPC seni takip etmeye başladı");
            SetWalkAnimation(true);
        }

        if (!isFollowing) return;

        // NavMesh dışına çıktıysa recovery dene — SetDestination ÇAĞIRMA
        if (!agent.isOnNavMesh)
        {
            if (!TrySnapToNavMesh())
            {
                // Kurtaramazsak bu karede atlıyoruz (spam önleme)
                return;
            }
        }

        // Throttle: her karede değil, ayarlı aralıkla
        if (Time.time < nextUpdateTime) return;
        nextUpdateTime = Time.time + destinationUpdateInterval;

        agent.SetDestination(player.position);
    }

    // ──────────────────────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (isCompleted) return;

        if (isFollowing && other.CompareTag("Ambulance"))
        {
            isCompleted = true;
            Debug.Log("[InjuredNPC] NPC ambulansa ulaştı");
            TaskManager.Instance?.NPCCured();
            Destroy(gameObject);
        }
    }

    // ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Ajan NavMesh üzerinde değilse en yakın geçerli noktaya warp eder.
    /// Başarılıysa true döner.
    /// </summary>
    private bool TrySnapToNavMesh()
    {
        if (agent == null) return false;

        // Zaten NavMesh üzerindeyse bir şey yapmaya gerek yok
        if (agent.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            Debug.Log($"[InjuredNPC] '{name}' NavMesh'e snap edildi: {hit.position}");
            return agent.isOnNavMesh;
        }

        Debug.LogWarning($"[InjuredNPC] '{name}' yakınında ({navMeshSampleRadius}m) NavMesh bulunamadı. " +
                         "NavMesh bake'i kontrol edin.", this);
        return false;
    }

    // ──────────────────────────────────────────────────────────────
    private void SetWalkAnimation(bool walking)
    {
        if (animator == null) return;
        animator.SetBool("walk", walking);
    }
}
