using UnityEngine;
using UnityEngine.Animations.Rigging;
using Unity.Cinemachine;
using StarterAssets;

/// <summary>
/// Sistema combinado de apuntado de linterna:
/// - Rota el personaje hacia la cámara
/// - Zoom de cámara al hombro
/// - Bloquea sprint
/// - IK sutil para apuntar verticalmente
/// - Bloquea la rotación del ThirdPersonController al apuntar
/// </summary>
public class FlashlightAimLock : MonoBehaviour
{
    [Header("Sistemas")]
    public FlashlightSystem      flashlight;
    public ThirdPersonController tpController;
    public StarterAssetsInputs   inputs;
    public Animator              animator;

    [Header("Cinemachine")]
    public CinemachineCamera tpCamera;

    [Header("Animation Rigging — IK del brazo")]
    public TwoBoneIKConstraint ikConstraint;
    public Transform           ikTarget;
    public Transform           ikHint;
    public Transform           gripPoint;

    [Header("Zoom de cámara")]
    public float normalCameraDistance = 4f;
    public float aimCameraDistance    = 2f;
    public float normalShoulderOffset = 1f;
    public float aimShoulderOffset    = 0.7f;
    public float zoomSpeed            = 6f;

    [Header("Rotación del personaje")]
    public float rotationSpeed = 12f;

    [Header("IK — Apuntado vertical")]
    public float aimDistance       = 2f;
    public float ikWeightSpeed     = 10f;
    public float ikFollowSpeed     = 15f;
    public float verticalAimLimit  = 60f;

    [Header("IK Hint — Posición del codo")]
    public float elbowRight   = 0.3f;
    public float elbowForward = 0.1f;
    public float elbowUp      = 0.5f;

    // ─── Estado ───────────────────────────────────────────────────────────────

    private bool _isAiming = false;
    private CinemachineThirdPersonFollow _tpFollow;

    // Hashes (calculados al inicio)
    private static readonly int HashIsAiming = Animator.StringToHash("IsAiming");
    private static readonly int HashMoveX    = Animator.StringToHash("MoveX");
    private static readonly int HashMoveY    = Animator.StringToHash("MoveY");

    // Comprobación de existencia de parámetros (evita errores si no están definidos)
    private bool _hasIsAimingParam = false;
    private bool _hasMoveXParam    = false;
    private bool _hasMoveYParam    = false;

    // ─── Init ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (flashlight   == null) flashlight   = GetComponentInChildren<FlashlightSystem>();
        if (tpController == null) tpController = GetComponent<ThirdPersonController>();
        if (inputs       == null) inputs       = GetComponent<StarterAssetsInputs>();
        if (animator     == null) animator     = GetComponentInChildren<Animator>();

        if (tpCamera != null)
            _tpFollow = tpCamera.GetComponent<CinemachineThirdPersonFollow>();

        if (flashlight != null)
            flashlight.OnAimingChanged += OnAimingChanged;

        if (ikConstraint != null) ikConstraint.weight = 0f;

        // Comprobar qué parámetros existen en el Animator
        CheckAnimatorParameters();
    }

    private void CheckAnimatorParameters()
    {
        if (animator == null) return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.nameHash == HashIsAiming) _hasIsAimingParam = true;
            if (param.nameHash == HashMoveX)    _hasMoveXParam    = true;
            if (param.nameHash == HashMoveY)    _hasMoveYParam    = true;
        }
    }

    private void OnDestroy()
    {
        if (flashlight != null)
            flashlight.OnAimingChanged -= OnAimingChanged;
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        UpdateCameraZoom();
        UpdateIKWeight();

        // Bloquear la rotación automática del ThirdPersonController al apuntar
        if (tpController != null)
            tpController.RotateTowardsMovement = !_isAiming;

        if (_isAiming)
        {
            RotatePlayerToCamera();
            BlockSprint();
            UpdateAimIK();
            UpdateMovementAnimator();
        }
        else
        {
            ResetMovementAnimator();
        }
    }

    // ─── Rotación del personaje ──────────────────────────────────────────────

    private void RotatePlayerToCamera()
    {
        if (Camera.main == null) return;

        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        if (cameraForward == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(cameraForward, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation,
            Time.deltaTime * rotationSpeed);
    }

    private void BlockSprint()
    {
        if (inputs != null) inputs.sprint = false;
    }

    // ─── Zoom de cámara ──────────────────────────────────────────────────────

    private void UpdateCameraZoom()
    {
        if (_tpFollow == null) return;

        float targetDistance = _isAiming ? aimCameraDistance : normalCameraDistance;
        float targetOffsetX  = _isAiming ? aimShoulderOffset : normalShoulderOffset;

        _tpFollow.CameraDistance = Mathf.Lerp(
            _tpFollow.CameraDistance, targetDistance, Time.deltaTime * zoomSpeed);

        Vector3 offset = _tpFollow.ShoulderOffset;
        offset.x = Mathf.Lerp(offset.x, targetOffsetX, Time.deltaTime * zoomSpeed);
        _tpFollow.ShoulderOffset = offset;
    }

    // ─── IK Vertical ─────────────────────────────────────────────────────────

    private void UpdateIKWeight()
    {
        if (ikConstraint == null) return;
        float target = _isAiming ? 1f : 0f;
        ikConstraint.weight = Mathf.Lerp(
            ikConstraint.weight, target, Time.deltaTime * ikWeightSpeed);
    }

    private void UpdateAimIK()
    {
        if (ikTarget == null || gripPoint == null || Camera.main == null) return;

        float verticalAngle = -Camera.main.transform.eulerAngles.x;
        if (verticalAngle < -180f) verticalAngle += 360f;
        if (verticalAngle >  180f) verticalAngle -= 360f;
        verticalAngle = Mathf.Clamp(verticalAngle, -verticalAimLimit, verticalAimLimit);

        Vector3 horizontalDir = transform.forward;
        Vector3 aimDir = Quaternion.AngleAxis(-verticalAngle, transform.right) * horizontalDir;

        Vector3 targetPos = gripPoint.position + aimDir * aimDistance;

        ikTarget.position = Vector3.Lerp(
            ikTarget.position, targetPos, Time.deltaTime * ikFollowSpeed);

        if (ikHint != null)
        {
            Vector3 hintPos = transform.position
                            + transform.right   * elbowRight
                            + transform.forward * elbowForward
                            + transform.up      * elbowUp;

            ikHint.position = Vector3.Lerp(
                ikHint.position, hintPos, Time.deltaTime * ikFollowSpeed);
        }
    }

    // ─── Animator — solo si los parámetros existen ───────────────────────────

    private void UpdateMovementAnimator()
    {
        if (animator == null || inputs == null) return;

        if (_hasIsAimingParam) animator.SetBool(HashIsAiming, true);
        if (_hasMoveXParam)    animator.SetFloat(HashMoveX, inputs.move.x);
        if (_hasMoveYParam)    animator.SetFloat(HashMoveY, inputs.move.y);
    }

    private void ResetMovementAnimator()
    {
        if (animator == null) return;

        if (_hasIsAimingParam) animator.SetBool(HashIsAiming, false);
        if (_hasMoveXParam)    animator.SetFloat(HashMoveX, 0f);
        if (_hasMoveYParam)    animator.SetFloat(HashMoveY, 0f);
    }

    // ─── Callback ────────────────────────────────────────────────────────────

    private void OnAimingChanged(bool aiming)
    {
        _isAiming = aiming;
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!_isAiming || ikTarget == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(ikTarget.position, 0.1f);
        Gizmos.color = Color.cyan;
        if (gripPoint != null)
            Gizmos.DrawLine(gripPoint.position, ikTarget.position);
    }
}