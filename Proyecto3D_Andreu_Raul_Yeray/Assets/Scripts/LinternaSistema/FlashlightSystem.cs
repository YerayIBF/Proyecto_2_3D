using UnityEngine;
using System.Collections;

public class FlashlightSystem : MonoBehaviour
{
    [Header("Luz principal")]
    public Light flashlightLight;
    public Camera mainCamera;

    [Header("Luz extra al apuntar")]
    public Light  aimSpotLight;
    public ParticleSystem aimParticles;

    [Header("Modo Normal")]
    public float normalIntensity = 1.5f;
    public float normalSpotAngle = 80f;
    public float normalRange     = 12f;

    [Header("Modo Apuntado")]
    public float aimIntensity   = 8f;
    public float aimSpotAngle   = 15f;
    public float aimRange       = 20f;
    public float transitionSpeed = 8f;

    [Header("Batería")]
    public float maxBattery     = 120f;
    public float currentBattery = 120f;
    public float drainRate      = 1f;
    public float aimDrainMultiplier = 3f;

    [Header("Intensidad por batería")]
    public float dimThreshold = 0.4f;

    [Header("Parpadeo natural")]
    public float flickerThreshold    = 0.15f;
    public float flickerOffIntensity = 0.05f;

    [Header("Detección de ojos (GLOBAL)")]
    [Tooltip("Layer que tienen los colliders de los ojos de los enemigos")]
    public LayerMask eyesLayerMask;
    public int       coneRayCount    = 8;
    public float     coneAngle       = 15f;
    public float     stunDetectRange = 10f;

    [Header("Recarga")]
    public GameObject drainedBatteriesPrefab;
    public Transform  dropPoint;
    public float      dropForce = 2f;

    // Estado
    private bool _isOn          = false;
    private bool _hasFlashlight = true;
    private bool _isAiming      = false;
    private bool _stunDetected  = false;

    private float _currentIntensity;
    private float _currentSpotAngle;
    private float _currentRange;

    private Coroutine _flickerCoroutine = null;
    private float     _flickerMultiplier = 1f;

    // Eventos
    public System.Action<float> OnBatteryChanged;
    public System.Action<bool>  OnFlashlightToggled;
    public System.Action<bool>  OnAimingChanged;

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (flashlightLight == null) flashlightLight = GetComponentInChildren<Light>();
        if (mainCamera      == null) mainCamera      = Camera.main;

        _currentIntensity = normalIntensity;
        _currentSpotAngle = normalSpotAngle;
        _currentRange     = normalRange;

        SetLight(false);

        if (aimSpotLight != null) aimSpotLight.enabled = false;
        if (aimParticles != null) aimParticles.Stop();
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_hasFlashlight) return;

        HandleInput();

        if (_isOn)
        {
            DrainBattery();
            UpdateFlickerState();
            UpdateLightParameters();
            UpdateAimEffects();
            DetectEyesStun();
        }
    }

    // ─── Input ───────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.F) && currentBattery > 0f)
            ToggleFlashlight();

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (PlayerStateMachine.Instance != null)
                PlayerStateMachine.Instance.TryReloadFlashlight();
            else
                Reload();
        }

        bool aimInput = Input.GetMouseButton(0) && _isOn;
        if (aimInput != _isAiming)
        {
            _isAiming = aimInput;
            OnAimingChanged?.Invoke(_isAiming);
        }
    }

    // ─── Parpadeo natural ────────────────────────────────────────────────────

    private void UpdateFlickerState()
    {
        float pct = currentBattery / maxBattery;

        if (pct <= flickerThreshold && _flickerCoroutine == null)
            _flickerCoroutine = StartCoroutine(FlickerRoutine());
        else if (pct > flickerThreshold && _flickerCoroutine != null)
        {
            StopCoroutine(_flickerCoroutine);
            _flickerCoroutine  = null;
            _flickerMultiplier = 1f;
        }
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            float pauseBetweenBursts = Random.Range(1.5f, 4f);
            yield return new WaitForSeconds(pauseBetweenBursts);

            int flickerCount = Random.Range(2, 5);
            for (int i = 0; i < flickerCount; i++)
            {
                _flickerMultiplier = Random.Range(0.05f, 0.2f);
                yield return new WaitForSeconds(Random.Range(0.04f, 0.1f));
                _flickerMultiplier = Random.Range(0.7f, 1f);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            }
            _flickerMultiplier = 1f;
        }
    }

    // ─── Parámetros de luz ────────────────────────────────────────────────────

    private void UpdateLightParameters()
    {
        float pct = currentBattery / maxBattery;

        float targetIntensity = _isAiming ? aimIntensity  : normalIntensity;
        float targetAngle     = _isAiming ? aimSpotAngle  : normalSpotAngle;
        float targetRange     = _isAiming ? aimRange      : normalRange;

        if (pct <= dimThreshold && pct > flickerThreshold)
        {
            float t = Mathf.InverseLerp(flickerThreshold, dimThreshold, pct);
            targetIntensity *= Mathf.Lerp(0.4f, 1f, t);
        }

        targetIntensity *= _flickerMultiplier;

        _currentIntensity = Mathf.Lerp(_currentIntensity, targetIntensity, Time.deltaTime * transitionSpeed);
        _currentSpotAngle = Mathf.Lerp(_currentSpotAngle, targetAngle,     Time.deltaTime * transitionSpeed);
        _currentRange     = Mathf.Lerp(_currentRange,     targetRange,     Time.deltaTime * transitionSpeed);

        flashlightLight.intensity = _currentIntensity;
        flashlightLight.spotAngle = _currentSpotAngle;
        flashlightLight.range     = _currentRange;
    }

    // ─── Efectos al apuntar ──────────────────────────────────────────────────

    private void UpdateAimEffects()
    {
        if (aimSpotLight != null && aimSpotLight.enabled != _isAiming)
            aimSpotLight.enabled = _isAiming;

        if (aimParticles != null)
        {
            if (_isAiming && !aimParticles.isPlaying)
                aimParticles.Play();
            else if (!_isAiming && aimParticles.isPlaying)
                aimParticles.Stop();
        }
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

        float currentDrain = _isAiming ? drainRate * aimDrainMultiplier : drainRate;
        currentBattery -= currentDrain * Time.deltaTime;
        currentBattery  = Mathf.Max(currentBattery, 0f);
        OnBatteryChanged?.Invoke(currentBattery / maxBattery);
    }

    // ─── Detección de ojos POR LAYER ─────────────────────────────────────────

    private void DetectEyesStun()
    {
        if (!_isAiming) return;
        if (mainCamera == null) return;

        Ray  ray     = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool hitting = false;

        EnemyBehaviourTree hitEnemy = TryRaycastEyes(ray.origin, ray.direction);

        if (hitEnemy == null)
        {
            for (int i = 0; i < coneRayCount; i++)
            {
                float   angle = (360f / coneRayCount) * i;
                Vector3 dir   = Quaternion.AngleAxis(angle, ray.direction)
                              * (Quaternion.AngleAxis(coneAngle * 0.5f,
                                 mainCamera.transform.right) * ray.direction);

                hitEnemy = TryRaycastEyes(ray.origin, dir);
                if (hitEnemy != null) break;
            }
        }

        if (hitEnemy != null)
        {
            hitting = true;
            if (!_stunDetected)
            {
                _stunDetected = true;
                hitEnemy.Stun();
                Debug.Log($"[Flashlight] ¡Stun! → {hitEnemy.gameObject.name}");
            }
        }
        else
        {
            _stunDetected = false;
        }
    }

    /// <summary>
    /// Devuelve el EnemyBehaviourTree del enemigo al que pertenecen los ojos
    /// que ha tocado el raycast, o null si no toca ningún ojo.
    /// </summary>
    private EnemyBehaviourTree TryRaycastEyes(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit,
                            stunDetectRange, eyesLayerMask))
        {
            // Buscar el EnemyBehaviourTree en el padre del collider impactado
            return hit.collider.GetComponentInParent<EnemyBehaviourTree>();
        }
        return null;
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

    public void ToggleFlashlight() => SetLight(!_isOn);

    private void SetLight(bool on)
    {
        _isOn = on;
        if (flashlightLight != null) flashlightLight.enabled = on;

        if (!on)
        {
            _isAiming = false;
            if (aimSpotLight != null) aimSpotLight.enabled = false;
            if (aimParticles != null) aimParticles.Stop();
        }

        OnFlashlightToggled?.Invoke(on);
    }

    public void PickupFlashlight() => _hasFlashlight = true;

    public float BatteryPercent => currentBattery / maxBattery;
    public bool  IsOn           => _isOn;
    public bool  IsAiming       => _isAiming;
    public bool  HasFlashlight  => _hasFlashlight;
}

