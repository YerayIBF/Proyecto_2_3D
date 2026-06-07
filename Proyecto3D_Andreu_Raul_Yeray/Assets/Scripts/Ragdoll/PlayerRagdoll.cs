using UnityEngine;

/// <summary>
/// Activa el ragdoll del personaje cuando muere.
/// </summary>
public class PlayerRagdoll : MonoBehaviour
{
    [Header("Referencias")]
    public Animator playerAnimator;
    public CharacterController characterController;

    [Tooltip("Si está activo, busca todos los Rigidbody hijos automáticamente al Start")]
    public bool autoFindRagdollParts = true;

    [Header("Partes del ragdoll (auto si está vacío)")]
    public Rigidbody[] ragdollRigidbodies;
    public Collider[]  ragdollColliders;

    [Header("Fuerza al morir")]
    public float deathForce = 2f;
    public Rigidbody pelvisRigidbody;

    private bool _ragdollActive = false;
    private bool _subscribed    = false;

    private void Awake()
    {
        if (playerAnimator == null) playerAnimator = GetComponentInChildren<Animator>();
        if (characterController == null) characterController = GetComponent<CharacterController>();

        if (autoFindRagdollParts)
        {
            ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
            ragdollColliders   = GetComponentsInChildren<Collider>();
            Debug.Log($"[Ragdoll] Encontrados {ragdollRigidbodies.Length} rigidbodies y {ragdollColliders.Length} colliders.");
        }

        SetRagdollActive(false);
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;

        if (PlayerStateMachine.Instance != null)
        {
            PlayerStateMachine.Instance.OnPlayerDied += ActivateRagdoll;
            _subscribed = true;
            Debug.Log("[Ragdoll] Suscrito a OnPlayerDied correctamente.");
        }
        else
        {
            Debug.LogWarning("[Ragdoll] PlayerStateMachine.Instance es null. Reintentando en Update...");
        }
    }

    private void Update()
    {
        if (!_subscribed) TrySubscribe();
    }

    private void OnDestroy()
    {
        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.OnPlayerDied -= ActivateRagdoll;
    }

    // ─── Activar ragdoll ─────────────────────────────────────────────────────

    public void ActivateRagdoll()
    {
        Debug.Log("[Ragdoll] ActivateRagdoll llamado.");

        if (_ragdollActive)
        {
            Debug.LogWarning("[Ragdoll] Ya estaba activo, saliendo.");
            return;
        }
        _ragdollActive = true;

        if (playerAnimator != null)
        {
            playerAnimator.enabled = false;
            Debug.Log("[Ragdoll] Animator desactivado.");
        }

        if (characterController != null)
        {
            characterController.enabled = false;
            Debug.Log("[Ragdoll] CharacterController desactivado.");
        }

        SetRagdollActive(true);

        if (pelvisRigidbody != null)
        {
            Vector3 forceDir = -transform.forward + Vector3.up * 0.3f;
            pelvisRigidbody.AddForce(forceDir * deathForce, ForceMode.VelocityChange);
            Debug.Log("[Ragdoll] Fuerza aplicada al pelvis.");
        }
        else
        {
            Debug.LogWarning("[Ragdoll] pelvisRigidbody es null, no se aplica fuerza.");
        }

        Debug.Log("[Ragdoll] Activado completamente.");
    }

    /// <summary>
    /// Desactiva el ragdoll y devuelve el control al Animator. Llamado al respawnear.
    /// </summary>
    public void DeactivateRagdoll()
    {
        if (!_ragdollActive) return;
        _ragdollActive = false;

        SetRagdollActive(false);

        if (playerAnimator != null)
            playerAnimator.enabled = true;

        if (characterController != null)
            characterController.enabled = true;

        Debug.Log("[Ragdoll] Desactivado (respawn).");
    }

    private void SetRagdollActive(bool active)
    {
        if (ragdollRigidbodies != null)
        {
            foreach (var rb in ragdollRigidbodies)
            {
                if (rb == null) continue;
                rb.isKinematic = !active;
                rb.useGravity  = active;
            }
        }

        if (ragdollColliders != null)
        {
            foreach (var col in ragdollColliders)
            {
                if (col == null) continue;
                if (col is CharacterController) continue;
                col.enabled = active;
            }
        }
    }

    // ─── Botón de prueba en el Inspector ─────────────────────────────────────

    [ContextMenu("TEST: Activar Ragdoll")]
    private void TestActivateRagdoll()
    {
        ActivateRagdoll();
    }
}