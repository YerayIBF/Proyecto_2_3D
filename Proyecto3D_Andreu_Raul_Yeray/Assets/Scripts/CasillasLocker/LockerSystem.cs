using UnityEngine;
using Unity.Cinemachine;
using StarterAssets;
using UnityEngine.InputSystem;

/// <summary>
/// v3 — Fixes:
///   1. Oculta el SkinnedMeshRenderer del personaje al entrar (elimina las líneas)
///   2. Restaura el mesh al salir
///   3. Expone IsHiding para que LockerInteractable bloquee la puerta
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class LockerSystem : MonoBehaviour
{
    [Header("Cinemachine (Unity 6 / CM 3.x)")]
    public CinemachineCamera firstPersonCamera;
    public CinemachineCamera thirdPersonCamera;
    public Transform headTransform;

    [Header("Mesh a ocultar al esconderse")]
    [Tooltip("Arrastra aquí todos los SkinnedMeshRenderer del personaje (cuerpo, ropa, etc.)")]
    public SkinnedMeshRenderer[] characterMeshes;

    [Header("Prioridades Cinemachine")]
    public int priorityHigh = 20;
    public int priorityLow  = 10;

    // Guardamos el target original de la cam 3P
    private Transform _originalThirdPersonTarget;

    // Componentes Starter Assets
    private ThirdPersonController _tpController;
    private StarterAssetsInputs   _inputs;
    private CharacterController   _charController;

    // Estado público para LockerInteractable y futuro Behaviour Tree
    public bool IsHiding { get; private set; } = false;
    public LockerInteractable CurrentLocker { get; private set; } = null;

    // ─── Awake ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        _tpController   = GetComponent<ThirdPersonController>();
        _inputs         = GetComponent<StarterAssetsInputs>();
        _charController = GetComponent<CharacterController>();

        if (thirdPersonCamera != null)
            _originalThirdPersonTarget = thirdPersonCamera.Target.TrackingTarget;

        if (firstPersonCamera != null)
            firstPersonCamera.Priority = priorityLow;

        // Auto-buscar meshes si no están asignados en el Inspector
        if (characterMeshes == null || characterMeshes.Length == 0)
            characterMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    public void EnterLocker(LockerInteractable locker, Transform hidePoint)
    {
        if (IsHiding) return;

        IsHiding      = true;
        CurrentLocker = locker;

        DisablePlayerMovement();

        // Mover al jugador al HidePoint
        _charController.enabled = false;
        transform.SetPositionAndRotation(hidePoint.position, hidePoint.rotation);
        _charController.enabled = true;

        // Ocultar mesh DESPUÉS de teletransportar (evita flash)
        SetMeshVisible(false);

        SwitchToFirstPerson();
    }

    public void ExitLocker()
    {
        if (!IsHiding) return;

        IsHiding      = false;
        CurrentLocker = null;

        // Restaurar mesh antes de cambiar cámara
        SetMeshVisible(true);

        SwitchToThirdPerson();
        Invoke(nameof(EnablePlayerMovement), 0.05f);
    }

    // ─── Mesh ────────────────────────────────────────────────────────────────

    private void SetMeshVisible(bool visible)
    {
        foreach (var mesh in characterMeshes)
            if (mesh != null) mesh.enabled = visible;
    }

    // ─── Cámara ──────────────────────────────────────────────────────────────

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

    // ─── Movimiento ──────────────────────────────────────────────────────────

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

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (headTransform == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(headTransform.position, 0.08f);
    }
}