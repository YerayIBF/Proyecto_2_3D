using UnityEngine;

/// <summary>
/// v3 — Fix principal: usa LayerMask para filtrar objetos lanzables
/// en lugar de intentar ignorar colliders del jugador manualmente.
///
/// SETUP OBLIGATORIO:
///   1. Edit → Project Settings → Tags and Layers → crea layer "Throwable"
///   2. Asigna ese layer a cada objeto lanzable (Cube, lata, piedra...)
///   3. En este script en el Inspector → Pickup Layer Mask → selecciona "Throwable"
/// </summary>
public class PlayerThrowSystem : MonoBehaviour
{
    [Header("Referencias")]
    public Transform holdPoint;
    public Camera mainCamera;

    [Header("Pickup")]
    public float throwForce   = 15f;
    public float pickupRange  = 3f;
    public float pickupRadius = 0.2f;

    [Tooltip("Selecciona SOLO el layer 'Throwable' aquí")]
    public LayerMask pickupLayerMask;

    [Header("Apuntado")]
    public float aimFov    = 50f;
    public float normalFov = 40f;
    public float fovSpeed  = 8f;

    // Estado
    private ThrowableObject _heldObject = null;
    private bool  _isAiming  = false;
    private float _currentFov;
    private LockerSystem _lockerSystem;

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _lockerSystem = GetComponent<LockerSystem>();
        if (mainCamera == null) mainCamera = Camera.main;
        _currentFov = normalFov;
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_lockerSystem != null && _lockerSystem.IsHiding) return;

        HandlePickup();
        HandleAiming();
        HandleThrow();
        UpdateFov();
        UpdateHoldPosition();
    }

    // ─── Recoger (E) ─────────────────────────────────────────────────────────

    private void HandlePickup()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        if (_heldObject != null) { DropObject(); return; }

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // SphereCast filtrando SOLO el layer Throwable — no choca con el jugador
        if (Physics.SphereCast(ray.origin, pickupRadius, ray.direction,
                               out RaycastHit hit, pickupRange, pickupLayerMask))
        {
            Debug.Log($"[Pickup] Detectado: {hit.collider.gameObject.name} a {hit.distance:F2}m");

            ThrowableObject throwable = hit.collider.GetComponent<ThrowableObject>();
            if (throwable != null && !throwable.isHeld)
                PickupObject(throwable);
            else
                Debug.Log("[Pickup] El objeto no tiene ThrowableObject o ya está cogido.");
        }
        else
        {
            Debug.Log($"[Pickup] Nada en rango. ¿Layer Mask configurado? Valor: {pickupLayerMask.value}");
        }
    }

    // ─── Apuntado (botón derecho) ─────────────────────────────────────────────

    private void HandleAiming()
    {
        if (_heldObject == null) { _isAiming = false; return; }
        _isAiming = Input.GetMouseButton(1);
    }

    // ─── Lanzar (botón izquierdo) ─────────────────────────────────────────────

    private void HandleThrow()
    {
        if (_heldObject == null) return;
        if (!Input.GetMouseButtonDown(0)) return;
        ThrowObject();
    }

    // ─── FOV ─────────────────────────────────────────────────────────────────

    private void UpdateFov()
    {
        float targetFov = _isAiming ? aimFov : normalFov;
        _currentFov = Mathf.Lerp(_currentFov, targetFov, Time.deltaTime * fovSpeed);
        if (mainCamera != null) mainCamera.fieldOfView = _currentFov;
    }

    // ─── Hold position ────────────────────────────────────────────────────────

    private void UpdateHoldPosition()
    {
        if (_heldObject == null || holdPoint == null) return;

        _heldObject.transform.position = Vector3.Lerp(
            _heldObject.transform.position, holdPoint.position, Time.deltaTime * 20f);
        _heldObject.transform.rotation = Quaternion.Lerp(
            _heldObject.transform.rotation, holdPoint.rotation, Time.deltaTime * 20f);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void PickupObject(ThrowableObject obj)
    {
        _heldObject = obj;
        _heldObject.OnPickup();
        _heldObject.transform.SetParent(holdPoint);
        Debug.Log($"[Pickup] ¡Recogido! → {obj.gameObject.name}");
        UIPromptManager.Instance?.Show("Click izq. para lanzar  ·  E para soltar");
    }

    private void ThrowObject()
    {
        _heldObject.transform.SetParent(null);
        _heldObject.OnThrow(mainCamera.transform.forward * throwForce);
        _heldObject = null;
        _isAiming   = false;
        mainCamera.fieldOfView = normalFov;
        UIPromptManager.Instance?.Hide();
    }

    private void DropObject()
    {
        _heldObject.transform.SetParent(null);
        _heldObject.OnDrop();
        _heldObject = null;
        _isAiming   = false;
        mainCamera.fieldOfView = normalFov;
        UIPromptManager.Instance?.Hide();
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (mainCamera == null) return;
        Gizmos.color = _heldObject != null ? Color.green : Color.yellow;
        Gizmos.DrawRay(mainCamera.transform.position,
                       mainCamera.transform.forward * pickupRange);
        if (holdPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(holdPoint.position, 0.08f);
        }
    }
}