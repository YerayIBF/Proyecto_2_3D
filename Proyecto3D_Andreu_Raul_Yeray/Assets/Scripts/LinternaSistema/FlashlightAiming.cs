using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Controla el IK del brazo para apuntar la linterna.
///
/// Funcionalidades:
/// - El brazo sigue al ratón (IK)
/// - El IKTarget rota la mano hacia el punto de apuntado (la luz lo sigue automáticamente)
/// - Si hay una pared cerca, baja la mano para evitar atravesarla
/// - Foco de luz extra y partículas al apuntar (gestionado por FlashlightSystem)
/// </summary>
public class FlashlightAiming : MonoBehaviour
{
    [Header("Animation Rigging")]
    public TwoBoneIKConstraint ikConstraint;
    public Transform ikTarget;
    public Transform ikHint;

    [Header("Referencias")]
    public FlashlightSystem flashlight;
    public Camera           aimCamera;

    [Header("Grip")]
    [Tooltip("Empty dentro de RightHand donde se sostiene la linterna")]
    public Transform gripPoint;

    [Header("Ajustes de apuntado")]
    public float aimDistance = 2f;
    public float followSpeed = 12f;

    [Header("Límite de ángulo")]
    [Range(30f, 90f)] public float maxAimAngle = 70f;

    [Header("Peso IK")]
    public float ikWeightSpeed = 10f;

    [Header("Raycast")]
    public LayerMask aimMask = ~0;

    [Header("Posición del codo")]
    public float elbowRight   = 0.8f;
    public float elbowForward = 0.6f;
    public float elbowUp      = 1.2f;

    [Header("Detección de paredes (bajar mano)")]
    [Tooltip("Distancia mínima a una pared para empezar a bajar la mano")]
    public float wallCheckDistance = 1.5f;
    [Tooltip("Layer Mask de las paredes que activan el bajado de mano")]
    public LayerMask wallLayerMask = ~0;
    [Tooltip("Velocidad con la que baja/sube la mano al detectar pared")]
    public float wallFadeSpeed = 8f;

    // Estado
    private bool    _isAiming    = false;
    private Vector3 _targetPos;
    private float   _wallProximityFactor = 0f; // 0 = libre, 1 = pared muy cerca

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (aimCamera  == null) aimCamera  = Camera.main;
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

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        UpdateWallProximity();
        UpdateIKWeight();

        if (_isAiming)
            UpdateAim();
    }

    // ─── Detectar paredes cercanas ───────────────────────────────────────────

    private void UpdateWallProximity()
    {
        if (gripPoint == null) return;

        // Raycast desde la mano hacia delante para ver si hay pared
        Vector3 forward = transform.forward;
        bool wallHit = Physics.Raycast(gripPoint.position, forward,
                                       out RaycastHit hit, wallCheckDistance, wallLayerMask);

        float targetFactor = wallHit
            ? Mathf.InverseLerp(wallCheckDistance, 0.3f, hit.distance)
            : 0f;

        // Suavizar la transición
        _wallProximityFactor = Mathf.Lerp(_wallProximityFactor, targetFactor,
                                          Time.deltaTime * wallFadeSpeed);
    }

    // ─── Peso del IK ─────────────────────────────────────────────────────────

    private void UpdateIKWeight()
    {
        // Cuando hay pared cerca, reducimos el peso del IK
        // (hace que la mano vuelva a la animación de reposo, "bajando" el brazo)
        float aimWeight = _isAiming ? 1f : 0f;
        float finalWeight = aimWeight * (1f - _wallProximityFactor);

        if (ikConstraint != null)
        {
            ikConstraint.weight = Mathf.Lerp(ikConstraint.weight, finalWeight,
                                             Time.deltaTime * ikWeightSpeed);
        }
    }

    // ─── Apuntado ────────────────────────────────────────────────────────────

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

        // Dirección desde la mano
        Vector3 origin = gripPoint.position;
        Vector3 aimDir = (worldTarget - origin).normalized;

        // Limitar ángulo
        Vector3 forward = transform.forward;
        float angle = Vector3.Angle(forward, aimDir);
        if (angle > maxAimAngle)
            aimDir = Vector3.Slerp(forward, aimDir, maxAimAngle / angle);

        // Posición final del IKTarget
        _targetPos = origin + aimDir * aimDistance;

        // Mover suavemente
        ikTarget.position = Vector3.Lerp(ikTarget.position, _targetPos,
                                         Time.deltaTime * followSpeed);

        // ROTAR EL IKTARGET hacia donde apunta — la mano hereda esta rotación
        // y como la linterna es hija de la mano, también gira con ella
        Quaternion targetRot = Quaternion.LookRotation(aimDir, Vector3.up);
        ikTarget.rotation = Quaternion.Lerp(ikTarget.rotation, targetRot,
                                            Time.deltaTime * followSpeed);

        // IK Hint (codo)
        if (ikHint != null)
        {
            Vector3 hintPos = transform.position
                            + transform.right   * elbowRight
                            + transform.forward * elbowForward
                            + transform.up      * elbowUp;

            ikHint.position = Vector3.Lerp(ikHint.position, hintPos,
                                           Time.deltaTime * followSpeed);
        }
    }

    // ─── Callback ────────────────────────────────────────────────────────────

    private void OnAimingChanged(bool aiming)
    {
        _isAiming = aiming;
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (gripPoint != null)
        {
            // Raycast de detección de pared
            Gizmos.color = _wallProximityFactor > 0.1f ? Color.red : Color.green;
            Gizmos.DrawRay(gripPoint.position, transform.forward * wallCheckDistance);
        }

        if (!_isAiming) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_targetPos, 0.1f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, _targetPos);
    }
}