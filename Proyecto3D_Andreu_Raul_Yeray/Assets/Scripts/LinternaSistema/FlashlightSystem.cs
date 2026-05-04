using UnityEngine;

/// <summary>
/// Sistema de linterna con dos modos:
///   NORMAL  → luz amplia, brazo estático, ilumina hacia adelante
///   AIMING  → luz estrecha e intensa, brazo sigue al ratón (gestionado por FlashlightAiming)
///
/// Coloca en el mismo GameObject que la Spot Light (hijo de RightHand).
/// </summary>
public class FlashlightSystem : MonoBehaviour
{
    // ─── Referencias ─────────────────────────────────────────────────────────

    [Header("Luz")]
    public Light flashlightLight;
    public Camera mainCamera;

    [Header("Modo Normal — luz amplia")]
    public float normalIntensity  = 1.5f;
    public float normalSpotAngle  = 80f;
    public float normalRange      = 12f;

    [Header("Modo Apuntado — luz estrecha e intensa")]
    public float aimIntensity     = 8f;
    public float aimSpotAngle     = 15f;
    public float aimRange         = 20f;

    [Tooltip("Velocidad de transición entre modos")]
    public float transitionSpeed  = 8f;

    [Header("Batería")]
    public float maxBattery       = 120f;
    public float currentBattery   = 120f;
    public float drainRate        = 1f;

    [Header("Intensidad según batería")]
    [Tooltip("% de batería a partir del cual empieza a bajar la intensidad")]
    public float dimThreshold     = 0.4f;

    [Header("Parpadeo batería baja")]
    public float flickerThreshold    = 0.15f;
    public float flickerSpeed        = 8f;
    public float flickerMinIntensity = 0.2f;

    [Header("Detección de ojos (stun)")]
    public Collider eyesCollider;
    public int      coneRayCount    = 8;
    public float    coneAngle       = 15f;
    public float    stunDetectRange = 10f;

    [Header("Recarga")]
    public GameObject drainedBatteriesPrefab;
    public Transform  dropPoint;
    public float      dropForce = 2f;

    // ─── Estado ───────────────────────────────────────────────────────────────

    private bool  _isOn          = false;
    private bool  _hasFlashlight = true;
    private bool  _isAiming      = false;
    private float _flickerTimer  = 0f;
    private bool  _stunDetected  = false;

    // Valores actuales interpolados
    private float _currentIntensity;
    private float _currentSpotAngle;
    private float _currentRange;

    private EnemyBehaviourTree _enemyBT;

    // Eventos para HUD y PlayerStateMachine
    public System.Action<float> OnBatteryChanged;
    public System.Action<bool>  OnFlashlightToggled;
    public System.Action<bool>  OnAimingChanged;

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (flashlightLight == null)
            flashlightLight = GetComponentInChildren<Light>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        _enemyBT = Object.FindFirstObjectByType<EnemyBehaviourTree>();

        _currentIntensity = normalIntensity;
        _currentSpotAngle = normalSpotAngle;
        _currentRange     = normalRange;

        SetLight(false);
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_hasFlashlight) return;

        HandleInput();

        if (_isOn)
        {
            DrainBattery();
            UpdateLightParameters();
            DetectEyesStun();
        }
    }

    // ─── Input ───────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        // F → encender/apagar
        if (Input.GetKeyDown(KeyCode.F) && currentBattery > 0f)
            ToggleFlashlight();

        // R → recargar (consume batería del PlayerStateMachine)
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (PlayerStateMachine.Instance != null)
                PlayerStateMachine.Instance.TryReloadFlashlight();
            else
                Reload(); // Fallback sin PlayerStateMachine
        }

        // Click derecho → modo apuntado
        bool aimInput = Input.GetMouseButton(1) && _isOn;
        if (aimInput != _isAiming)
        {
            _isAiming = aimInput;
            OnAimingChanged?.Invoke(_isAiming);
        }
    }

    // ─── Parámetros de luz — transición suave entre modos ────────────────────

    private void UpdateLightParameters()
    {
        float batteryPct = currentBattery / maxBattery;

        // Valores objetivo según modo
        float targetIntensity = _isAiming ? aimIntensity  : normalIntensity;
        float targetAngle     = _isAiming ? aimSpotAngle  : normalSpotAngle;
        float targetRange     = _isAiming ? aimRange      : normalRange;

        // Modificar intensidad según nivel de batería
        if (batteryPct <= flickerThreshold)
        {
            // Parpadeo
            _flickerTimer += Time.deltaTime * flickerSpeed;
            float flicker  = Mathf.PerlinNoise(_flickerTimer, 0f);
            targetIntensity = Mathf.Lerp(flickerMinIntensity,
                                         targetIntensity * 0.4f, flicker);
        }
        else if (batteryPct <= dimThreshold)
        {
            // Reducir progresivamente
            float t = Mathf.InverseLerp(0f, dimThreshold, batteryPct);
            targetIntensity = Mathf.Lerp(targetIntensity * 0.2f, targetIntensity, t);
        }

        // Interpolar suavemente
        _currentIntensity = Mathf.Lerp(_currentIntensity, targetIntensity,
                                        Time.deltaTime * transitionSpeed);
        _currentSpotAngle = Mathf.Lerp(_currentSpotAngle, targetAngle,
                                        Time.deltaTime * transitionSpeed);
        _currentRange     = Mathf.Lerp(_currentRange, targetRange,
                                        Time.deltaTime * transitionSpeed);

        // Aplicar a la luz
        flashlightLight.intensity  = _currentIntensity;
        flashlightLight.spotAngle  = _currentSpotAngle;
        flashlightLight.range      = _currentRange;
    }

    // ─── Batería ─────────────────────────────────────────────────────────────

    private void DrainBattery()
    {
        if (currentBattery <= 0f)
        {
            currentBattery = 0f;
            SetLight(false);
            OnBatteryChanged?.Invoke(0f);
            return;
        }

        currentBattery -= drainRate * Time.deltaTime;
        currentBattery  = Mathf.Max(currentBattery, 0f);
        OnBatteryChanged?.Invoke(currentBattery / maxBattery);
    }

    // ─── Detección de ojos ────────────────────────────────────────────────────

    private void DetectEyesStun()
    {
        // Solo detecta stun en modo apuntado
        if (!_isAiming || eyesCollider == null || _enemyBT == null) return;

        Ray   ray     = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool  hitting = false;

        if (RaycastHitsEyes(ray.origin, ray.direction))
            hitting = true;

        if (!hitting)
        {
            for (int i = 0; i < coneRayCount; i++)
            {
                float   angle = (360f / coneRayCount) * i;
                Vector3 dir   = Quaternion.AngleAxis(angle, ray.direction)
                              * (Quaternion.AngleAxis(coneAngle * 0.5f,
                                 mainCamera.transform.right) * ray.direction);
                if (RaycastHitsEyes(ray.origin, dir)) { hitting = true; break; }
            }
        }

        if (hitting && !_stunDetected)
        {
            _stunDetected = true;
            _enemyBT.Stun();
            Debug.Log("[Flashlight] ¡Stun!");
        }
        else if (!hitting)
        {
            _stunDetected = false;
        }
    }

    private bool RaycastHitsEyes(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, stunDetectRange))
            return hit.collider == eyesCollider;
        return false;
    }

    // ─── Recarga ──────────────────────────────────────────────────────────────

    public void Reload()
    {
        if (currentBattery >= maxBattery) return;

        if (drainedBatteriesPrefab != null && dropPoint != null)
        {
            GameObject dropped = Instantiate(drainedBatteriesPrefab,
                                             dropPoint.position, dropPoint.rotation);
            Rigidbody rb = dropped.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dir = dropPoint.forward * 0.5f + Vector3.down;
                rb.AddForce(dir.normalized * dropForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * dropForce, ForceMode.Impulse);
            }
            Destroy(dropped, 10f);
        }

        currentBattery = maxBattery;
        OnBatteryChanged?.Invoke(1f);

        if (!_isOn) SetLight(true);
        Debug.Log("[Flashlight] Recargada.");
    }

    public void AddBattery(float amount)
    {
        currentBattery = Mathf.Min(currentBattery + amount, maxBattery);
        OnBatteryChanged?.Invoke(currentBattery / maxBattery);
    }

    // ─── Toggle ──────────────────────────────────────────────────────────────

    public void ToggleFlashlight()
    {
        SetLight(!_isOn);
    }

    private void SetLight(bool on)
    {
        _isOn = on;
        if (flashlightLight != null)
            flashlightLight.enabled = on;

        if (!on) _isAiming = false;

        OnFlashlightToggled?.Invoke(on);
        CrosshairHUD.Instance?.SetState(on
            ? CrosshairHUD.CrosshairState.Flashlight
            : CrosshairHUD.CrosshairState.Normal);
    }

    public void PickupFlashlight()
    {
        _hasFlashlight = true;
        Debug.Log("[Flashlight] Recogida.");
    }

    // ─── Propiedades ─────────────────────────────────────────────────────────

    public float BatteryPercent => currentBattery / maxBattery;
    public bool  IsOn           => _isOn;
    public bool  IsAiming       => _isAiming;
    public bool  HasFlashlight  => _hasFlashlight;
}
