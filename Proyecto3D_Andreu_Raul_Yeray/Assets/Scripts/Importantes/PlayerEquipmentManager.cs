using UnityEngine;

/// <summary>
/// Manager central de equipamiento del jugador.
/// Coordina dónde está cada item y bloquea acciones incompatibles.
/// </summary>
public class PlayerEquipmentManager : MonoBehaviour
{
    public static PlayerEquipmentManager Instance { get; private set; }

    // ─── Anclajes ─────────────────────────────────────────────────────────────

    [Header("Anclajes — Linterna")]
    public Transform linternaAnchor_Hand;
    public Transform linternaAnchor_Shoulder;

    [Header("Anclajes — Megáfono")]
    public Transform megafonoAnchor_Hand;
    public Transform megafonoAnchor_Back;

    [Header("Anclajes — Objetos lanzables")]
    public Transform throwableAnchor_Hand;

    // ─── Items físicos ───────────────────────────────────────────────────────

    [Header("Items físicos")]
    public GameObject linternaObject;
    public GameObject megafonoObject;

    // ─── Estado ───────────────────────────────────────────────────────────────

    public enum EquipmentState
    {
        Normal,
        AimingFlashlight,
        AimingMegaphone,
        HoldingObject
    }

    public EquipmentState CurrentState { get; private set; } = EquipmentState.Normal;

    private bool _hasFlashlight = true;
    private bool _hasMegaphone  = false;
    private GameObject _heldThrowable = null;

    // ─── Reglas de bloqueo ───────────────────────────────────────────────────

    /// <summary>
    /// El FlashlightSystem consulta esto antes de activar el apuntado.
    /// Devuelve false si el megáfono está apuntando o se está usando un objeto.
    /// </summary>
    public bool CanAimFlashlight
    {
        get
        {
            // No se puede apuntar la linterna si el megáfono ya está apuntando
            if (CurrentState == EquipmentState.AimingMegaphone) return false;
            return _hasFlashlight;
        }
    }

    /// <summary>
    /// El MegaphoneSystem consulta esto antes de activar el apuntado.
    /// Devuelve false si la linterna está apuntando o se está llevando un objeto.
    /// </summary>
    public bool CanAimMegaphone
    {
        get
        {
            if (!_hasMegaphone) return false;
            // No se puede apuntar el megáfono si la linterna ya está apuntando
            if (CurrentState == EquipmentState.AimingFlashlight) return false;
            // Tampoco se puede si lleva un objeto en la mano
            if (CurrentState == EquipmentState.HoldingObject) return false;
            return true;
        }
    }

    /// <summary>
    /// El PlayerThrowSystem consulta esto antes de recoger un objeto.
    /// </summary>
    public bool CanPickupThrowable
    {
        get
        {
            // No se puede recoger objetos mientras apuntas algo
            if (CurrentState == EquipmentState.AimingFlashlight) return false;
            if (CurrentState == EquipmentState.AimingMegaphone)  return false;
            return true;
        }
    }

    // ─── Init ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        ApplyState(EquipmentState.Normal);
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    public void PickupMegaphone(GameObject megaphone)
    {
        megafonoObject = megaphone;
        _hasMegaphone  = true;
        ApplyState(CurrentState);
    }

    /// <summary>
    /// Llamado por FlashlightSystem. Solo cambia el estado si CanAimFlashlight es true.
    /// Devuelve true si se aplicó el cambio.
    /// </summary>
    public bool SetAimingFlashlight(bool aiming)
    {
        if (aiming)
        {
            if (!CanAimFlashlight) return false;
            ChangeState(EquipmentState.AimingFlashlight);
        }
        else if (CurrentState == EquipmentState.AimingFlashlight)
        {
            ChangeState(EquipmentState.Normal);
        }
        return true;
    }

    /// <summary>
    /// Llamado por MegaphoneSystem. Solo cambia si CanAimMegaphone es true.
    /// </summary>
    public bool SetAimingMegaphone(bool aiming)
    {
        if (aiming)
        {
            if (!CanAimMegaphone) return false;
            ChangeState(EquipmentState.AimingMegaphone);
        }
        else if (CurrentState == EquipmentState.AimingMegaphone)
        {
            ChangeState(EquipmentState.Normal);
        }
        return true;
    }

    public bool PickupThrowable(GameObject throwable)
    {
        if (!CanPickupThrowable) return false;
        _heldThrowable = throwable;
        ChangeState(EquipmentState.HoldingObject);
        return true;
    }

    public void ReleaseThrowable()
    {
        _heldThrowable = null;
        ChangeState(EquipmentState.Normal);
    }

    // ─── Cambio de estado ─────────────────────────────────────────────────────

    private void ChangeState(EquipmentState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        ApplyState(newState);
        Debug.Log($"[Equipment] Estado → {newState}");
    }

    private void ApplyState(EquipmentState state)
    {
        switch (state)
        {
            case EquipmentState.Normal:
                AttachItem(linternaObject, linternaAnchor_Hand);
                if (_hasMegaphone) AttachItem(megafonoObject, megafonoAnchor_Hand);
                break;

            case EquipmentState.AimingFlashlight:
                AttachItem(linternaObject, linternaAnchor_Hand);
                if (_hasMegaphone) AttachItem(megafonoObject, megafonoAnchor_Hand);
                break;

            case EquipmentState.AimingMegaphone:
                AttachItem(linternaObject, linternaAnchor_Shoulder);
                if (_hasMegaphone) AttachItem(megafonoObject, megafonoAnchor_Hand);
                break;

            case EquipmentState.HoldingObject:
                AttachItem(linternaObject, linternaAnchor_Hand);
                if (_hasMegaphone) AttachItem(megafonoObject, megafonoAnchor_Back);
                if (_heldThrowable != null)
                    AttachItem(_heldThrowable, throwableAnchor_Hand);
                break;
        }
    }

    private void AttachItem(GameObject item, Transform anchor)
    {
        if (item == null || anchor == null) return;
        item.transform.SetParent(anchor, false);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
    }

    // ─── Consultas ────────────────────────────────────────────────────────────

    public bool HasFlashlight       => _hasFlashlight;
    public bool HasMegaphone        => _hasMegaphone;
    public bool IsHoldingThrowable  => _heldThrowable != null;
    public bool IsAimingFlashlight  => CurrentState == EquipmentState.AimingFlashlight;
    public bool IsAimingMegaphone   => CurrentState == EquipmentState.AimingMegaphone;
}