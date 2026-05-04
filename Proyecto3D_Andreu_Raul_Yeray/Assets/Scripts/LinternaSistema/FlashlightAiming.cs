using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Controla el IK del brazo para apuntar la linterna correctamente.
/// Versión estable con límite de ángulo y filtro de capa.
/// </summary>
public class FlashlightAiming : MonoBehaviour
{
    [Header("Animation Rigging")]
    public TwoBoneIKConstraint ikConstraint;
    public Transform ikTarget;
    public Transform ikHint;

    [Header("Referencias")]
    public FlashlightSystem flashlight;
    public Camera aimCamera;

    [Header("Grip (MUY IMPORTANTE)")]
    public Transform gripPoint; // Empty dentro de RightHand

    [Header("Ajustes")]
    public float aimDistance = 2f;
    public float followSpeed = 12f;

    [Header("Límite de ángulo")]
    [Range(30f, 90f)] public float maxAimAngle = 70f;

    [Header("Peso IK")]
    public float ikWeightSpeed = 10f;

    [Header("Raycast")]
    public LayerMask aimMask = ~0; // Asigna en el Inspector: todo menos la capa del jugador

    [Header("Posición del codo (ajusta para estirar el brazo)")]
    public float elbowRight = 0.8f;
    public float elbowForward = 0.6f;
    public float elbowUp = 1.2f;

    private bool _isAiming = false;
    private Vector3 _targetPos;

    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (aimCamera == null) aimCamera = Camera.main;
        if (flashlight == null) flashlight = GetComponentInChildren<FlashlightSystem>();

        if (flashlight != null)
            flashlight.OnAimingChanged += OnAimingChanged;

        if (ikConstraint != null)
            ikConstraint.weight = 0f;
    }

    private void OnDestroy()
    {
        if (flashlight != null)
            flashlight.OnAimingChanged -= OnAimingChanged;
    }

    // ─────────────────────────────────────────────────────────────

    private void Update()
    {
        UpdateIKWeight();

        if (_isAiming)
            UpdateAim();
    }

    // ─────────────────────────────────────────────────────────────

    private void UpdateIKWeight()
    {
        float target = _isAiming ? 1f : 0f;

        if (ikConstraint != null)
        {
            ikConstraint.weight = Mathf.Lerp(
                ikConstraint.weight,
                target,
                Time.deltaTime * ikWeightSpeed
            );
        }
    }

    // ─────────────────────────────────────────────────────────────

    private void UpdateAim()
    {
        if (aimCamera == null || ikTarget == null || gripPoint == null) return;

        // Rayo desde el centro de la cámara
        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 worldTarget;

        if (Physics.Raycast(ray, out RaycastHit hit, 50f, aimMask))
            worldTarget = hit.point;
        else
            worldTarget = ray.origin + ray.direction * aimDistance;

        // Dirección desde la mano REAL
        Vector3 origin = gripPoint.position;
        Vector3 aimDir = (worldTarget - origin).normalized;

        // ─── Limitar el ángulo de apuntado ─────────────────
        Vector3 forward = transform.forward;
        float angle = Vector3.Angle(forward, aimDir);

        if (angle > maxAimAngle)
        {
            // Forzar la dirección al borde del cono
            aimDir = Vector3.Slerp(forward, aimDir, maxAimAngle / angle);
        }

        // Posición final del IK
        _targetPos = origin + aimDir * aimDistance;

        // Movimiento suave
        ikTarget.position = Vector3.Lerp(
            ikTarget.position,
            _targetPos,
            Time.deltaTime * followSpeed
        );

        // ─── IK HINT (codo estable) ─────────────────────────

        if (ikHint != null)
        {
            Vector3 hintPos =
                transform.position +
                transform.right * elbowRight +
                transform.forward * elbowForward +
                transform.up * elbowUp;

            ikHint.position = Vector3.Lerp(
                ikHint.position,
                hintPos,
                Time.deltaTime * followSpeed
            );
        }
    }

    // ─────────────────────────────────────────────────────────────

    private void OnAimingChanged(bool aiming)
    {
        _isAiming = aiming;
    }

    // ─────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!_isAiming) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_targetPos, 0.1f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, _targetPos);
    }
}