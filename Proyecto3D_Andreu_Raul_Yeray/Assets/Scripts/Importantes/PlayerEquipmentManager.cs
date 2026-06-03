using UnityEngine;

/// <summary>
/// Manager central de equipamiento.
///
/// REGLAS:
/// - Mano dcha: linterna O megáfono (alterna con Q)
/// - Mano izq: solo lanzables
/// - Si llevas lanzable → NO puedes apuntar nada
/// - Si apuntas → NO puedes cambiar de mano ni recoger
/// </summary>
public class PlayerEquipmentManager : MonoBehaviour
{
    public static PlayerEquipmentManager Instance { get; private set; }

    [Header("Anclajes — Linterna")]
    public Transform linternaAnchor_Hand;
    public Transform linternaAnchor_Shoulder;

    [Header("Anclajes — Megáfono")]
    public Transform megafonoAnchor_Hand;
    public Transform megafonoAnchor_Back;

    [Header("Anclajes — Objetos lanzables")]
    public Transform throwableAnchor_Hand;

    [Header("Items físicos (se asignan al recogerlos)")]
    public GameObject linternaObject;
    public GameObject megafonoObject;

    [Header("Offset — Linterna en mano")]
    public Vector3 linternaHandPos = Vector3.zero;
    public Vector3 linternaHandRot = Vector3.zero;

    [Header("Offset — Linterna en hombro")]
    public Vector3 linternaShoulderPos = Vector3.zero;
    public Vector3 linternaShoulderRot = Vector3.zero;

    [Header("Offset — Megáfono en mano")]
    public Vector3 megafonoHandPos = Vector3.zero;
    public Vector3 megafonoHandRot = Vector3.zero;

    [Header("Offset — Megáfono en espalda")]
    public Vector3 megafonoBackPos = Vector3.zero;
    public Vector3 megafonoBackRot = Vector3.zero;

    [Header("Inicio")]
    public bool startWithFlashlight = false;

    [Header("Tecla de cambio")]
    public KeyCode swapKey = KeyCode.Q;

    public enum RightHandItem { Flashlight, Megaphone }
    public RightHandItem CurrentRightHand { get; private set; } = RightHandItem.Flashlight;

    private bool _hasFlashlight = false;
    private bool _hasMegaphone  = false;
    private bool _aimingFlashlight = false;
    private bool _aimingMegaphone  = false;
    private GameObject _heldThrowable = null;

    // ─── Reglas de bloqueo ───────────────────────────────────────────────────

    public bool CanAimFlashlight =>
        _hasFlashlight
        && CurrentRightHand == RightHandItem.Flashlight
        && !_aimingMegaphone
        && _heldThrowable == null;          // ← NUEVO: no apuntar con lanzable

    public bool CanAimMegaphone =>
        _hasMegaphone
        && CurrentRightHand == RightHandItem.Megaphone
        && !_aimingFlashlight
        && _heldThrowable == null;          // ← NUEVO

    public bool CanPickupThrowable =>
        _heldThrowable == null
        && !_aimingFlashlight
        && !_aimingMegaphone;

    public bool CanSwapHand =>
        _hasFlashlight
        && _hasMegaphone
        && !_aimingFlashlight
        && !_aimingMegaphone
        && _heldThrowable == null;          // ← NUEVO: tampoco cambiar de mano

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Si el flag startWithFlashlight está activado Y hay linterna asignada
        if (startWithFlashlight && linternaObject != null)
        {
            _hasFlashlight = true;

            // Activar la linterna y desactivar su física/colliders
            linternaObject.SetActive(true);
            DisablePhysicsOf(linternaObject);

            // Avisar al FlashlightSystem que ya está recogida
            FlashlightSystem fs = linternaObject.GetComponentInChildren<FlashlightSystem>();
            if (fs != null) fs.PickupFlashlight();
        }

        ApplyState();
    }

    private void Update()
    {
        if (Input.GetKeyDown(swapKey) && CanSwapHand)
            SwapRightHand();
    }

    // ─── Cambio de mano ──────────────────────────────────────────────────────

    public void SwapRightHand()
    {
        if (!CanSwapHand) return;

        CurrentRightHand = (CurrentRightHand == RightHandItem.Flashlight)
            ? RightHandItem.Megaphone
            : RightHandItem.Flashlight;

        ApplyState();
        Debug.Log($"[Equipment] Mano dcha → {CurrentRightHand}");
    }

    // ─── Recoger items ───────────────────────────────────────────────────────

    public void PickupFlashlight(GameObject flashlight)
    {
        if (_hasFlashlight) return;

        linternaObject = flashlight;
        _hasFlashlight = true;
        linternaObject.SetActive(true);
        DisablePhysicsOf(linternaObject);

        CurrentRightHand = RightHandItem.Flashlight;

        FlashlightSystem fs = linternaObject.GetComponentInChildren<FlashlightSystem>();
        if (fs != null) fs.PickupFlashlight();

        ApplyState();
        Debug.Log("[Equipment] Linterna recogida.");
    }

    public void PickupMegaphone(GameObject megaphone)
    {
        if (_hasMegaphone) return;

        megafonoObject = megaphone;
        _hasMegaphone  = true;
        DisablePhysicsOf(megafonoObject);

        CurrentRightHand = RightHandItem.Megaphone;

        ApplyState();
        Debug.Log("[Equipment] Megáfono recogido.");
    }

    private void DisablePhysicsOf(GameObject item)
    {
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity  = false;
            rb.isKinematic = true;
        }

        Collider col = item.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    // ─── Apuntado ────────────────────────────────────────────────────────────

    public bool SetAimingFlashlight(bool aiming)
    {
        if (aiming && !CanAimFlashlight) return false;
        _aimingFlashlight = aiming;
        return true;
    }

    public bool SetAimingMegaphone(bool aiming)
    {
        if (aiming && !CanAimMegaphone) return false;
        _aimingMegaphone = aiming;
        return true;
    }

    // ─── Objetos lanzables ───────────────────────────────────────────────────

    public bool PickupThrowable(GameObject throwable)
    {
        if (!CanPickupThrowable) return false;
        _heldThrowable = throwable;
        return true;
    }

    public void ReleaseThrowable() => _heldThrowable = null;

    // ─── Aplicar estado físico ───────────────────────────────────────────────

    private void ApplyState()
    {
        // Linterna
        if (_hasFlashlight && linternaObject != null)
        {
            if (CurrentRightHand == RightHandItem.Flashlight)
                AttachItem(linternaObject, linternaAnchor_Hand,
                           linternaHandPos, linternaHandRot);
            else
                AttachItem(linternaObject, linternaAnchor_Shoulder,
                           linternaShoulderPos, linternaShoulderRot);
        }

        // Megáfono
        if (_hasMegaphone && megafonoObject != null)
        {
            if (CurrentRightHand == RightHandItem.Megaphone)
                AttachItem(megafonoObject, megafonoAnchor_Hand,
                           megafonoHandPos, megafonoHandRot);
            else
                AttachItem(megafonoObject, megafonoAnchor_Back,
                           megafonoBackPos, megafonoBackRot);
        }
    }

    private void AttachItem(GameObject item, Transform anchor,
                            Vector3 localPos, Vector3 localRot)
    {
        if (item == null || anchor == null) return;
        item.transform.SetParent(anchor, false);
        item.transform.localPosition = localPos;
        item.transform.localRotation = Quaternion.Euler(localRot);
    }

    // ─── Consultas ───────────────────────────────────────────────────────────

    public bool HasFlashlight       => _hasFlashlight;
    public bool HasMegaphone        => _hasMegaphone;
    public bool IsHoldingThrowable  => _heldThrowable != null;
    public bool IsAimingFlashlight  => _aimingFlashlight;
    public bool IsAimingMegaphone   => _aimingMegaphone;
    public bool IsAiming            => _aimingFlashlight || _aimingMegaphone;
    public bool IsFlashlightInHand  => CurrentRightHand == RightHandItem.Flashlight && _hasFlashlight;
    public bool IsMegaphoneInHand   => CurrentRightHand == RightHandItem.Megaphone && _hasMegaphone;
}