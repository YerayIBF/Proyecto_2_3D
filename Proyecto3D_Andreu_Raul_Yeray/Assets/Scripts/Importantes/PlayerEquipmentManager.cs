using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manager central de equipamiento.
///
/// REGLAS:
/// - Mano dcha: linterna O megáfono (alterna con Q / D-Pad)
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

    [Header("Tecla de cambio (teclado)")]
    public KeyCode swapKey = KeyCode.Q;

    [Header("Input Actions (mando) — D-Pad")]
    [Tooltip("Botón Next del D-Pad (derecha) para cambiar de arma")]
    public InputActionReference nextAction;
    [Tooltip("Botón Previous del D-Pad (izquierda) para cambiar de arma")]
    public InputActionReference previousAction;

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
        && _heldThrowable == null;

    public bool CanAimMegaphone =>
        _hasMegaphone
        && CurrentRightHand == RightHandItem.Megaphone
        && !_aimingFlashlight
        && _heldThrowable == null;

    public bool CanPickupThrowable =>
        _heldThrowable == null
        && !_aimingFlashlight
        && !_aimingMegaphone;

    public bool CanSwapHand =>
        _hasFlashlight
        && _hasMegaphone
        && !_aimingFlashlight
        && !_aimingMegaphone
        && _heldThrowable == null;

    // ─── Input Actions enable/disable ────────────────────────────────────────

    private void OnEnable()
    {
        if (nextAction != null)     nextAction.action.Enable();
        if (previousAction != null) previousAction.action.Enable();
    }

    private void OnDisable()
    {
        if (nextAction != null)     nextAction.action.Disable();
        if (previousAction != null) previousAction.action.Disable();
    }

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (startWithFlashlight && linternaObject != null)
        {
            _hasFlashlight = true;

            linternaObject.SetActive(true);
            DisablePhysicsOf(linternaObject);

            FlashlightSystem fs = linternaObject.GetComponentInChildren<FlashlightSystem>();
            if (fs != null) fs.PickupFlashlight();
        }

        ApplyState();
    }

    private void Update()
    {
        if (PlayerStateMachine.Instance != null && !PlayerStateMachine.Instance.IsAlive)
            return;

        // Cambiar de mano (Q teclado o D-Pad izq/dch mando)
        bool swapInput = Input.GetKeyDown(swapKey)
                      || (nextAction != null && nextAction.action.WasPressedThisFrame())
                      || (previousAction != null && previousAction.action.WasPressedThisFrame());

        if (swapInput && CanSwapHand)
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