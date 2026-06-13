using UnityEngine;
using Unity.Cinemachine;
using StarterAssets;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class LockerSystem : MonoBehaviour
{
    [Header("Cinemachine (Unity 6 / CM 3.x)")]
    public CinemachineCamera firstPersonCamera;
    public CinemachineCamera thirdPersonCamera;
    public Transform headTransform;

    [Header("Mesh a ocultar al esconderse")]
    public SkinnedMeshRenderer[] characterMeshes;

    [Header("Prioridades Cinemachine")]
    public int priorityHigh = 20;
    public int priorityLow  = 10;

    [Header("UI feedback (opcional)")]
    public GameObject blockedExitWarning;
    public float blockedWarningDuration = 1.5f;
    private float _blockedWarningTimer = 0f;

    private Transform _originalThirdPersonTarget;

    private ThirdPersonController _tpController;
    private StarterAssetsInputs   _inputs;
    private CharacterController   _charController;

    public bool IsHiding { get; private set; } = false;
    public LockerInteractable CurrentLocker { get; private set; } = null;

    private void Awake()
    {
        _tpController   = GetComponent<ThirdPersonController>();
        _inputs         = GetComponent<StarterAssetsInputs>();
        _charController = GetComponent<CharacterController>();

        if (thirdPersonCamera != null)
            _originalThirdPersonTarget = thirdPersonCamera.Target.TrackingTarget;

        if (firstPersonCamera != null)
            firstPersonCamera.Priority = priorityLow;

        if (characterMeshes == null || characterMeshes.Length == 0)
            characterMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();

        if (blockedExitWarning != null)
            blockedExitWarning.SetActive(false);
    }

    private void Update()
    {
        if (_blockedWarningTimer > 0f)
        {
            _blockedWarningTimer -= Time.deltaTime;
            if (_blockedWarningTimer <= 0f && blockedExitWarning != null)
                blockedExitWarning.SetActive(false);
        }
    }

    public void EnterLocker(LockerInteractable locker, Transform hidePoint)
    {
        if (IsHiding) return;

        NotifyEnemiesOfHidingBeforeTeleport(locker);

        IsHiding      = true;
        CurrentLocker = locker;

        DisablePlayerMovement();

        _charController.enabled = false;
        transform.SetPositionAndRotation(hidePoint.position, hidePoint.rotation);
        _charController.enabled = true;

        SetMeshVisible(false);
        SwitchToFirstPerson();
    }

    private void NotifyEnemiesOfHidingBeforeTeleport(LockerInteractable locker)
    {
        EnemyBehaviourTree[] enemies = Object.FindObjectsByType<EnemyBehaviourTree>(FindObjectsSortMode.None);

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            if (enemy.IsSeeingPlayer)
            {
                enemy._knownLockerWithPlayer = locker;
                Debug.Log($"[Locker] Enemigo {enemy.name} vio al jugador esconderse en {locker.name}");
            }
            else
            {
                Debug.Log($"[Locker] Enemigo {enemy.name} NO te vio entrar — investigará por su cuenta");
            }
        }
    }

    public bool ExitLocker()
    {
        if (!IsHiding) return false;

        if (IsBlockedByEnemy())
        {
            Debug.Log("[Locker] ¡No puedes salir! El enemigo viene a por ti.");

            if (blockedExitWarning != null)
            {
                blockedExitWarning.SetActive(true);
                _blockedWarningTimer = blockedWarningDuration;
            }

            return false;
        }

        IsHiding      = false;
        CurrentLocker = null;

        SetMeshVisible(true);
        SwitchToThirdPerson();
        Invoke(nameof(EnablePlayerMovement), 0.05f);

        return true;
    }

    /// <summary>
    /// Salida forzada por muerte. Cierra la taquilla y saca al jugador.
    /// </summary>
    public void ForceExitOnDeath()
    {
        if (!IsHiding) return;

        // Cerrar la puerta de la taquilla para que vuelva a funcionar al respawnear
        if (CurrentLocker != null)
        {
            CurrentLocker.CloseDoor();
            CurrentLocker.ForceReset();
        }

        // Limpiar el "conocimiento" de los enemigos sobre esta taquilla
        EnemyBehaviourTree[] enemies = Object.FindObjectsByType<EnemyBehaviourTree>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null) enemy._knownLockerWithPlayer = null;
        }

        IsHiding      = false;
        CurrentLocker = null;

        SetMeshVisible(true);
        SwitchToThirdPerson();

        Debug.Log("[Locker] Salida forzada por muerte. Taquilla cerrada y reseteada.");
    }

    private bool IsBlockedByEnemy()
    {
        EnemyBehaviourTree[] enemies = Object.FindObjectsByType<EnemyBehaviourTree>(FindObjectsSortMode.None);

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            if (enemy.HasKnownLockerWithPlayer
                && enemy._knownLockerWithPlayer == CurrentLocker)
            {
                return true;
            }
        }

        return false;
    }

    private void SetMeshVisible(bool visible)
    {
        foreach (var mesh in characterMeshes)
            if (mesh != null) mesh.enabled = visible;
    }

    private void SwitchToFirstPerson()
    {
        if (firstPersonCamera != null)
        {
            firstPersonCamera.Target.TrackingTarget = headTransform;
            firstPersonCamera.Priority = priorityHigh;
        }
        if (thirdPersonCamera != null)
            thirdPersonCamera.Priority = priorityLow;
    }

    private void SwitchToThirdPerson()
    {
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.Target.TrackingTarget = _originalThirdPersonTarget;
            thirdPersonCamera.Priority = priorityHigh;
        }
        if (firstPersonCamera != null)
            firstPersonCamera.Priority = priorityLow;
    }

    private void DisablePlayerMovement()
    {
        if (_tpController != null) _tpController.enabled = false;
        if (_inputs != null)
        {
            _inputs.move   = Vector2.zero;
            _inputs.look   = Vector2.zero;
            _inputs.jump   = false;
            _inputs.sprint = false;
        }
    }

    private void EnablePlayerMovement()
    {
        if (_tpController != null) _tpController.enabled = true;
    }

    private void OnDrawGizmos()
    {
        if (headTransform == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(headTransform.position, 0.08f);
    }
}