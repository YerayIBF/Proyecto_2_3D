using UnityEngine;
using UnityEngine.Animations.Rigging;

public class FlashlightAiming : MonoBehaviour
{
    [Header("Animation Rigging")]
    public TwoBoneIKConstraint ikConstraint;
    public MultiAimConstraint  handAimConstraint;
    public Transform ikTarget;
    public Transform ikHint;

    [Header("Referencias")]
    public FlashlightSystem flashlight;
    public Camera           aimCamera;

    [Header("Grip")]
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
    public float elbowRight   = 0.4f;
    public float elbowForward = 0.2f;
    public float elbowUp      = 0.7f;

    [Header("Detección de paredes")]
    [Tooltip("Distancia desde el pecho del jugador a la pared para empezar a bajar la mano")]
    public float wallCheckDistance = 1.2f;
    public LayerMask wallLayerMask = ~0;
    public float wallFadeSpeed     = 8f;
    [Tooltip("Altura del centro del pecho desde transform.position")]
    public float chestHeight = 1.4f;
    [Tooltip("Número de raycasts en abanico para detectar paredes en distintos ángulos")]
    public int wallRayCount = 5;
    [Tooltip("Ángulo total del abanico de detección")]
    public float wallFanAngle = 60f;

    // Estado
    private bool    _isAiming = false;
    private Vector3 _targetPos;
    private float   _wallProximityFactor = 0f;

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (aimCamera  == null) aimCamera  = Camera.main;
        if (flashlight == null) flashlight = GetComponentInChildren<FlashlightSystem>();

        if (flashlight != null)
            flashlight.OnAimingChanged += OnAimingChanged;

        if (ikConstraint != null)      ikConstraint.weight      = 0f;
        if (handAimConstraint != null) handAimConstraint.weight = 0f;
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
        UpdateConstraintWeights();

        if (_isAiming)
            UpdateAim();
    }

    // ─── Detección de paredes mejorada ───────────────────────────────────────

    private void UpdateWallProximity()
    {
        // Origen FIJO en el pecho del jugador (no en la mano que se mueve)
        Vector3 chestOrigin = transform.position + Vector3.up * chestHeight;

        // Lanzar varios raycasts en abanico horizontal
        float closestHit = wallCheckDistance + 1f;
        bool  anyHit     = false;

        for (int i = 0; i < wallRayCount; i++)
        {
            // Distribuir los rayos en el abanico
            float angle = -wallFanAngle * 0.5f + (wallFanAngle / (wallRayCount - 1)) * i;
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;

            if (Physics.Raycast(chestOrigin, dir,
                                out RaycastHit hit, wallCheckDistance, wallLayerMask))
            {
                anyHit = true;
                if (hit.distance < closestHit)
                    closestHit = hit.distance;
            }
        }

        // Calcular el factor de proximidad usando la distancia más cercana
        float targetFactor = anyHit
            ? Mathf.InverseLerp(wallCheckDistance, 0.3f, closestHit)
            : 0f;

        _wallProximityFactor = Mathf.Lerp(_wallProximityFactor, targetFactor,
                                          Time.deltaTime * wallFadeSpeed);
    }

    // ─── Pesos de los constraints ────────────────────────────────────────────

    private void UpdateConstraintWeights()
    {
        float aimWeight   = _isAiming ? 1f : 0f;
        float finalWeight = aimWeight * (1f - _wallProximityFactor);

        if (ikConstraint != null)
            ikConstraint.weight = Mathf.Lerp(ikConstraint.weight, finalWeight,
                                             Time.deltaTime * ikWeightSpeed);

        if (handAimConstraint != null)
            handAimConstraint.weight = Mathf.Lerp(handAimConstraint.weight, finalWeight,
                                                   Time.deltaTime * ikWeightSpeed);
    }

    // ─── Apuntado ────────────────────────────────────────────────────────────

    private void UpdateAim()
    {
        if (aimCamera == null || ikTarget == null || gripPoint == null) return;

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 worldTarget;
        if (Physics.Raycast(ray, out RaycastHit hit, 50f, aimMask))
            worldTarget = hit.point;
        else
            worldTarget = ray.origin + ray.direction * aimDistance;

        Vector3 origin = gripPoint.position;
        Vector3 aimDir = (worldTarget - origin).normalized;

        Vector3 forward = transform.forward;
        float angle = Vector3.Angle(forward, aimDir);
        if (angle > maxAimAngle)
            aimDir = Vector3.Slerp(forward, aimDir, maxAimAngle / angle);

        _targetPos = origin + aimDir * aimDistance;

        ikTarget.position = Vector3.Lerp(ikTarget.position, _targetPos,
                                         Time.deltaTime * followSpeed);

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

    private void OnAimingChanged(bool aiming) => _isAiming = aiming;

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        // Visualizar abanico de detección de paredes
        Vector3 chestOrigin = transform.position + Vector3.up * chestHeight;
        Gizmos.color = _wallProximityFactor > 0.1f ? Color.red : Color.green;

        for (int i = 0; i < wallRayCount; i++)
        {
            float angle = -wallFanAngle * 0.5f + (wallFanAngle / (wallRayCount - 1)) * i;
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            Gizmos.DrawRay(chestOrigin, dir * wallCheckDistance);
        }

        if (!_isAiming) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_targetPos, 0.1f);
    }
}