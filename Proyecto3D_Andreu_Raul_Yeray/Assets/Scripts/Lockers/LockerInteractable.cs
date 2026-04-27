using System.Collections;
using UnityEngine;

public class LockerInteractable : MonoBehaviour
{
    [Header("Referencias")]
    public Transform door;
    public Transform hidePoint;
    public Transform exitPoint;

    [Header("Puerta")]
    public float doorOpenAngle = -110f;
    public float doorSpeed     = 200f;

    [Header("Tiempos")]
    public float exitDelay    = 0.45f;
    public float exitCooldown = 1.5f;

    [Header("Detección")]
    public float interactRange = 1.8f;

    // Estado
    private float _currentAngle      = 0f;
    private float _targetAngle       = 0f;
    private bool  _inputBlocked      = false;
    private bool  _isRunningSequence = false;

    private Transform    _playerTransform;
    private LockerSystem _lockerSystem;

    public bool IsOccupied { get; private set; } = false;

    // ─── Registro estático — solo una taquilla activa a la vez ───────────────
    private static LockerInteractable _nearestLocker = null;
    public void OpenDoor()  => SetDoor(open: true);
    public void CloseDoor() => SetDoor(open: false);
    // ─── Init ────────────────────────────────────────────────────────────────

    [Header("Transparencia de puerta")]
    public LockerDoorTransparency doorTransparency;

    private void Start()
    {
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            _playerTransform = playerGO.transform;
            _lockerSystem    = playerGO.GetComponentInParent<LockerSystem>()
                            ?? playerGO.GetComponent<LockerSystem>();
        }
         if (doorTransparency == null)
        {
             doorTransparency = GetComponent<LockerDoorTransparency>();
        }
       
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        // Animar puerta
        if (!Mathf.Approximately(_currentAngle, _targetAngle))
        {
            _currentAngle = Mathf.MoveTowards(_currentAngle, _targetAngle, doorSpeed * Time.deltaTime);
            door.localEulerAngles = new Vector3(0f, _currentAngle, 0f);
        }

        if (_playerTransform == null) return;

        float dist   = Vector3.Distance(transform.position, _playerTransform.position);
        bool inRange = dist <= interactRange;

        // Actualizar cuál es la taquilla más cercana
        UpdateNearestLocker(inRange, dist);

        // Solo la taquilla más cercana gestiona el prompt y el input
        bool isNearest = (_nearestLocker == this);

        // Ocultar si no soy la más cercana o estoy fuera de rango
        if (!inRange || !isNearest)
        {
            if (_nearestLocker != this)
                UIPromptManager.Instance?.Hide();
            return;
        }

        // A partir de aquí soy la más cercana y estoy en rango
        if (_inputBlocked || _isRunningSequence) return;

        // Prompt
        if (!IsOccupied)
            UIPromptManager.Instance?.Show("Pulsa [E] para esconderte");
        else if (IsOccupied && _lockerSystem?.CurrentLocker == this)
            UIPromptManager.Instance?.Show("Pulsa [E] para salir");

        // Input E
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!IsOccupied)
                StartCoroutine(EnterSequence());
            else if (_lockerSystem?.CurrentLocker == this)
                StartCoroutine(ExitSequence());
        }
    }

    private void UpdateNearestLocker(bool inRange, float myDist)
    {
        if (!inRange)
        {
            // Si yo era la más cercana y salgo del rango, limpiar
            if (_nearestLocker == this)
            {
                _nearestLocker = null;
                UIPromptManager.Instance?.Hide();
            }
            return;
        }

        // Estoy en rango — ¿soy más cercana que la actual?
        if (_nearestLocker == null)
        {
            _nearestLocker = this;
        }
        else if (_nearestLocker != this)
        {
            float nearestDist = Vector3.Distance(
                _nearestLocker.transform.position, _playerTransform.position);

            if (myDist < nearestDist)
                _nearestLocker = this;
        }
    }

    // ─── Secuencias ──────────────────────────────────────────────────────────

    private IEnumerator EnterSequence()
{
    _isRunningSequence = true;
    IsOccupied = true;
    UIPromptManager.Instance?.Hide();

    SetDoor(open: true);
    yield return new WaitForSeconds(0.35f);

    _lockerSystem.EnterLocker(this, hidePoint);

    yield return new WaitForSeconds(0.2f);
    SetDoor(open: false);

    // --- Activar transparencia ---
    if (doorTransparency != null)
        doorTransparency.SetTransparent();
    // -----------------------------

    _isRunningSequence = false;
}

    private IEnumerator ExitSequence()
{
    _isRunningSequence = true;
    _inputBlocked      = true;

    // --- Restaurar opacidad antes de abrir la puerta ---
    if (doorTransparency != null)
        doorTransparency.SetOpaque();
    // ---------------------------------------------------

    SetDoor(open: true);
    yield return new WaitForSeconds(exitDelay);

    IsOccupied = false;

    if (exitPoint != null)
    {
        var cc = _lockerSystem.GetComponent<CharacterController>();
        cc.enabled = false;
        _lockerSystem.transform.SetPositionAndRotation(
            exitPoint.position, exitPoint.rotation);
        cc.enabled = true;
    }

    _lockerSystem.ExitLocker();
    UIPromptManager.Instance?.Hide();

    yield return new WaitForSeconds(0.5f);
    SetDoor(open: false);

    yield return new WaitForSeconds(exitCooldown);

    _inputBlocked      = false;
    _isRunningSequence = false;
    _nearestLocker     = null;
    UIPromptManager.Instance?.Hide();
}

    // ─── Helper ──────────────────────────────────────────────────────────────

    private void SetDoor(bool open)
    {
        _targetAngle = open ? doorOpenAngle : 0f;
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        bool isNearest = (_nearestLocker == this);
        Gizmos.color = isNearest ? Color.green : Color.white;
        Gizmos.DrawWireSphere(transform.position, interactRange);

        if (hidePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(hidePoint.position, 0.15f);
        }
        if (exitPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(exitPoint.position, 0.15f);
            Gizmos.DrawLine(transform.position, exitPoint.position);
        }
    }
}