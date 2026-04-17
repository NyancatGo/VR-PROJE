using UnityEngine;
using System.Collections;

/// <summary>
/// NPC'yi oturma pozisyonuna getirir.
/// Kemiklerin gerçek pozisyonlarından yön hesaplayarak doğru rotasyonu bulur.
/// Tüm Humanoid rig'lerde çalışır.
/// </summary>
public class NPCSitPose : MonoBehaviour
{
    [Header("Pozisyon")]
    [Tooltip("NPC'yi dikey olarak aşağı kaydır (sandalyeye otursun)")]
    public float verticalOffset = -0.35f;

    void Start()
    {
        StartCoroutine(ApplyPoseNextFrame());
    }

    private IEnumerator ApplyPoseNextFrame()
    {
        yield return null;

        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogWarning($"[NPCSitPose] {gameObject.name}: Humanoid Animator bulunamadı!");
            yield break;
        }

        // Animator'ı durdur — T-pose'u dondur
        animator.enabled = false;

        Transform root = animator.transform;
        Vector3 forward = root.forward;
        Vector3 up      = root.up;
        Vector3 right   = root.right;

        // ==================== BACAKLAR ====================
        // Üst bacaklar: aşağı bakıyorlar → öne (forward) yönüne çevir
        Transform leftUpper  = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform leftLower  = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        Transform leftFoot   = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightUpper = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        Transform rightLower = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        Transform rightFoot  = animator.GetBoneTransform(HumanBodyBones.RightFoot);

        // Oturma yönü: üst bacak ileri-aşağı, alt bacak düz aşağı
        Vector3 sitUpperDir = (forward * 0.85f - up * 0.15f).normalized;
        Vector3 sitLowerDir = (-up).normalized;

        RotateBoneToDirection(leftUpper, leftLower, sitUpperDir);
        RotateBoneToDirection(rightUpper, rightLower, sitUpperDir);
        RotateBoneToDirection(leftLower, leftFoot, sitLowerDir);
        RotateBoneToDirection(rightLower, rightFoot, sitLowerDir);

        // ==================== KOLLAR ====================
        // Kollar: T-pose'da yana bakıyorlar → aşağıya indir
        Transform leftArm   = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        Transform leftFore  = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        Transform leftHand  = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform rightArm  = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        Transform rightFore = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

        // Kolları aşağı + hafif öne indir
        Vector3 leftArmDir  = (-up * 0.9f + forward * 0.1f - right * 0.15f).normalized;
        Vector3 rightArmDir = (-up * 0.9f + forward * 0.1f + right * 0.15f).normalized;

        RotateBoneToDirection(leftArm, leftFore, leftArmDir);
        RotateBoneToDirection(rightArm, rightFore, rightArmDir);

        // Ön kolları hafif öne bük (eller dizlere doğru)
        Vector3 forearmDir = (-up * 0.5f + forward * 0.5f).normalized;
        RotateBoneToDirection(leftFore, leftHand, forearmDir);
        RotateBoneToDirection(rightFore, rightHand, forearmDir);

        // ==================== GÖVDE ====================
        Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        if (spine != null)
        {
            // Gövdeyi çok hafif öne eğ
            spine.rotation = Quaternion.AngleAxis(3f, right) * spine.rotation;
        }

        // ==================== POZİSYON ====================
        transform.position += new Vector3(0f, verticalOffset, 0f);

        // ==================== MESH KORUMA ====================
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            smr.updateWhenOffscreen = true;
        }

        Debug.Log($"[NPCSitPose] ✓ {gameObject.name} oturma pozuna getirildi.");
    }

    /// <summary>
    /// Bir kemiği, mevcut yönünden hedef yöne döndürür.
    /// bone = döndürülecek kemik, child = bir sonraki kemik (yön hesabı için)
    /// </summary>
    private void RotateBoneToDirection(Transform bone, Transform child, Vector3 targetWorldDir)
    {
        if (bone == null || child == null) return;

        // Kemiğin şu anki yönü = child'a doğru
        Vector3 currentDir = (child.position - bone.position).normalized;

        // Mevcut yönden hedef yöne dönüş
        Quaternion delta = Quaternion.FromToRotation(currentDir, targetWorldDir);

        // Uygula
        bone.rotation = delta * bone.rotation;
    }
}
