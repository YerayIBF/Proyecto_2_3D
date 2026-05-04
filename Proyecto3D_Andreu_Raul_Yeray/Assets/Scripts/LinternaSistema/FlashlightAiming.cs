using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Controla el IK del brazo derecho cuando el jugador apunta con la linterna.
/// Requiere: Animation Rigging instalado, Two Bone IK Constraint configurado.
///
/// Coloca en PlayerArmature.
///
/// Setup Inspector:
///   ikConstraint  → el Two Bone IK Constraint del brazo derecho
///   ikTarget      → el Transform "IKTarget" (hijo del Rig)
///   ikHint        → el Transform "IKHint" (codo)
///   flashlight    → el FlashlightSystem
///   aimCamera     → Main Camera
/// </summary>
public class FlashlightAiming : MonoBehaviour
{
    [Header("Animation Rigging")]
    public TwoBoneIKConstraint ikConstraint;
    public Transform           ikTarget;
    public Transform           ikHint;

    [Header("Referencias")]
    public FlashlightSystem flashlight;
    public Camera           aimCamera;

    [Header("Movimiento del IKTarget")]
    [Tooltip("Distancia a la que se coloca el IKTarget desde la cámara")]
    public float aimDistance     = 5f;
    [Tooltip("Velocidad con la que el brazo sigue al ratón")]
    public float followSpeed     = 12f;
    [Tooltip("Límite vertical de apuntado (grados arriba/abajo)")]
    public float verticalLimit   = 60f;
    [Tooltip("Límite horizontal de apuntado (grados izquierda/derecha)")]
    public float horizontalLimit = 70f;

    [Header("Peso del IK")]
    [Tooltip("Velocidad de transición del peso del IK (0=brazo libre, 1=IK activo)")]
    public float ikWeightSpeed   = 10f;

    // Estado
    private bool  _isAiming      = false;
    private float _targetWeight   = 0f;
    private Vector3 _targetPos;

    // Posición neutra del IKTarget (brazo hacia adelante en reposo)
    private Vector3 _restPosition;

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (aimCamera   == null) aimCamera   = Camera.main;
        if (flashlight  == null) flashlight  = GetComponentInChildren<FlashlightSystem>();

        // Suscribirse al evento de apuntado del FlashlightSystem
        if (flashlight != null)
            flashlight.OnAimingChanged += OnAimingChanged;

        // IK empieza con peso 0
        if (ikConstraint != null)
            ikConstraint.weight = 0f;
    }

    private void Start()
    {
        // Guardar posición de reposo del IKTarget
        if (ikTarget != null)
            _restPosition = ikTarget.position;

        _targetPos = _restPosition;
    }

    private void OnDestroy()
    {
        if (flashlight != null)
            flashlight.OnAimingChanged -= OnAimingChanged;
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        UpdateIKWeight();

        if (_isAiming)
            UpdateAimTarget();
    }

    // ─── Peso del IK ─────────────────────────────────────────────────────────

    private void UpdateIKWeight()
    {
        float targetWeight = _isAiming ? 1f : 0f;

        if (ikConstraint != null)
        {
            ikConstraint.weight = Mathf.Lerp(
                ikConstraint.weight, targetWeight,
                Time.deltaTime * ikWeightSpeed);
        }
    }

    // ─── Mover IKTarget con el ratón ─────────────────────────────────────────

    private void UpdateAimTarget()
    {
        if (aimCamera == null || ikTarget == null) return;

        // Raycast desde el centro de la cámara
        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 worldTarget;
        if (Physics.Raycast(ray, out RaycastHit hit, aimDistance * 10f))
            worldTarget = hit.point;
        else
            worldTarget = ray.origin + ray.direction * aimDistance;

        // Limitar el ángulo de apuntado respecto al forward del jugador
        Vector3 dirToTarget   = (worldTarget - transform.position).normalized;
        Vector3 playerForward = transform.forward;

        // Ángulo horizontal
        float horizontalAngle = Vector3.SignedAngle(
            new Vector3(playerForward.x, 0f, playerForward.z),
            new Vector3(dirToTarget.x,   0f, dirToTarget.z),
            Vector3.up);

        // Ángulo vertical
        float verticalAngle = Vector3.SignedAngle(
            new Vector3(0f, playerForward.y, playerForward.z),
            new Vector3(0f, dirToTarget.y,   dirToTarget.z),
            Vector3.right);

        // Aplicar límites
        horizontalAngle = Mathf.Clamp(horizontalAngle, -horizontalLimit, horizontalLimit);
        verticalAngle   = Mathf.Clamp(verticalAngle,   -verticalLimit,   verticalLimit);

        // Recalcular dirección limitada
        Quaternion limitedRot = Quaternion.Euler(-verticalAngle, horizontalAngle, 0f);
        Vector3 limitedDir    = limitedRot * playerForward;

        // Posición objetivo del IKTarget
        _targetPos = transform.position + limitedDir * aimDistance;

        // Mover IKTarget suavemente
        ikTarget.position = Vector3.Lerp(
            ikTarget.position, _targetPos,
            Time.deltaTime * followSpeed);

        // IKHint — mantener el codo hacia afuera
        if (ikHint != null)
        {
            Vector3 hintPos = transform.position
                            + transform.right * 0.5f
                            + transform.up    * 0.3f;
            ikHint.position = Vector3.Lerp(
                ikHint.position, hintPos,
                Time.deltaTime * followSpeed);
        }
    }

    // ─── Callback ────────────────────────────────────────────────────────────

    private void OnAimingChanged(bool isAiming)
    {
        _isAiming = isAiming;

        if (!isAiming && ikTarget != null)
        {
            // Al dejar de apuntar, IKTarget vuelve a la posición de reposo
            _targetPos = _restPosition;
        }
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!_isAiming || aimCamera == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_targetPos, 0.1f);
        Gizmos.DrawLine(transform.position, _targetPos);
    }
}
