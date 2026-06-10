using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Sistema de linterna.
///
/// - F / Y (mando) = encender/apagar
/// - R / RT (mando) = recargar
/// - Click DERECHO / RB (mando) = apuntar
/// </summary>
public class FlashlightSystem : MonoBehaviour
{
    [Header("Pickup")]
    public bool startPickedUp = false;
    public float pickupRange = 2f;
    public GameObject pickupIcon;

    [Header("Luz")]
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
    public float maxBattery        = 120f;
    public float currentBattery    = 120f;
    public float drainRate         = 1f;
    public float aimDrainMultiplier = 3f;

    [Header("Intensidad por batería")]
    public float dimThreshold = 0.4f;

    [Header("Parpadeo natural")]
    public float flickerThreshold = 0.15f;

    [Header("Detección de ojos (por TAG)")]
    public string eyesTag = "EnemyEyes";
    [Tooltip("Radio del SphereCast principal para detectar ojos (más permisivo)")]
    public float    stunSphereRadius = 1f;
    [Tooltip("Número de rayos en cono para detección periférica")]
    public int      coneRayCount    = 16;
    [Tooltip("Ángulo del cono de detección de ojos en grados")]
    public float    coneAngle       = 25f;
    [Tooltip("Distancia máxima del stun")]
    public float    stunDetectRange = 12f;

    [Header("Input Actions (opcional, para mando)")]
    public InputActionReference toggleFlashlightAction;
    public InputActionReference reloadAction;
    public InputActionReference apuntarAction;

    // ─── Estado ───────────────────────────────────────────────────────────────

    private bool _isOn          = false;
    private bool _hasFlashlight = false;
    private bool _isAiming      = false;
    private bool _stunDetected  = false;
    private bool _playerInRange = false;
    private Transform _player;

    private float _currentIntensity;
    private float _currentSpotAngle;
    private float _currentRange;

    private Coroutine _flickerCoroutine = null;
    private float     _flickerMultiplier = 1f;

    // Eventos
    public System.Action<float> OnBatteryChanged;
    public System.Action<bool>  OnFlashlightToggled;
    public System.Action<bool>  OnAimingChanged;

    // ─── Input Actions enable/disable ────────────────────────────────────────

    private void OnEnable()
    {
        if (toggleFlashlightAction != null) toggleFlashlightAction.action.Enable();
        if (reloadAction != null)           reloadAction.action.Enable();
        if (apuntarAction != null)          apuntarAction.action.Enable();
    }

    private void OnDisable()
    {
        if (toggleFlashlightAction != null) toggleFlashlightAction.action.Disable();
        if (reloadAction != null)           reloadAction.action.Disable();
        if (apuntarAction != null)          apuntarAction.action.Disable();
    }

    private void Awake()
    {
        if (flashlightLight == null) flashlightLight = GetComponentInChildren<Light>();
        if (mainCamera      == null) mainCamera      = Camera.main;

        _currentIntensity = normalIntensity;
        _currentSpotAngle = normalSpotAngle;
        _currentRange     = normalRange;

        if (aimSpotLight != null) aimSpotLight.enabled = false;
        if (aimParticles != null) aimParticles.Stop();
        if (pickupIcon != null) pickupIcon.SetActive(false);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;

        if (flashlightLight != null) flashlightLight.enabled = false;
    }

    private void Start()
    {
        _hasFlashlight = startPickedUp;
    }

    private void Update()
    {
        if (PlayerStateMachine.Instance != null && !PlayerStateMachine.Instance.IsAlive)
            return;

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

    public void PickupFlashlight()
    {
        _hasFlashlight = true;
        if (pickupIcon != null) pickupIcon.SetActive(false);
    }

    private void HandleInput()
    {
        if (PlayerStateMachine.Instance != null && PlayerStateMachine.Instance.IsHiding)
        {
            if (_isOn) SetLight(false);
            return;
        }

        bool toggleInput = Input.GetKeyDown(KeyCode.F)
                        || (toggleFlashlightAction != null && toggleFlashlightAction.action.WasPressedThisFrame());

        if (toggleInput && currentBattery > 0f)
            ToggleFlashlight();

        bool reloadInput = Input.GetKeyDown(KeyCode.R)
                        || (reloadAction != null && reloadAction.action.WasPressedThisFrame());

        if (reloadInput)
        {
            TriggerReloadAnimation();
        }

        bool canAim = _isOn;
        if (PlayerEquipmentManager.Instance != null)
            canAim = canAim && PlayerEquipmentManager.Instance.CanAimFlashlight;

        bool aimInputPressed = Input.GetMouseButton(1)
                            || (apuntarAction != null && apuntarAction.action.IsPressed());

        bool aimInput = aimInputPressed && canAim;

        if (aimInput != _isAiming)
        {
            _isAiming = aimInput;
            _stunDetected = false; // Reset al cambiar de aim
            OnAimingChanged?.Invoke(_isAiming);
            PlayerEquipmentManager.Instance?.SetAimingFlashlight(_isAiming);
        }
    }

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
            yield return new WaitForSeconds(Random.Range(1.5f, 4f));
            int count = Random.Range(2, 5);
            for (int i = 0; i < count; i++)
            {
                _flickerMultiplier = Random.Range(0.05f, 0.2f);
                yield return new WaitForSeconds(Random.Range(0.04f, 0.1f));
                _flickerMultiplier = Random.Range(0.7f, 1f);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            }
            _flickerMultiplier = 1f;
        }
    }

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

    private void UpdateAimEffects()
    {
        if (aimSpotLight != null && aimSpotLight.enabled != _isAiming)
            aimSpotLight.enabled = _isAiming;

        if (aimParticles != null)
        {
            if (_isAiming && !aimParticles.isPlaying) aimParticles.Play();
            else if (!_isAiming && aimParticles.isPlaying) aimParticles.Stop();
        }
    }

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

    // ─── DETECCIÓN DE OJOS MEJORADA (SphereCast + cono) ──────────────────────

    private void DetectEyesStun()
    {
        if (!_isAiming || mainCamera == null) return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // 1) Probar primero con SphereCast central (más permisivo que Raycast)
        EnemyBehaviourTree hitEnemy = TrySphereCastEyes(ray.origin, ray.direction);
        enemigoaire hitAire = TrySphereCastEyesAire(ray.origin, ray.direction);

        // 2) Si no detecta nada, probar rayos en cono para visión periférica
        if (hitEnemy == null && hitAire == null)
        {
            for (int i = 0; i < coneRayCount; i++)
            {
                float angle = (360f / coneRayCount) * i;
                Quaternion rot = Quaternion.AngleAxis(angle, ray.direction);
                Vector3 offsetDir = Quaternion.AngleAxis(coneAngle * 0.5f, mainCamera.transform.right) * ray.direction;
                Vector3 dir = rot * offsetDir;

                if (hitEnemy == null) hitEnemy = TrySphereCastEyes(ray.origin, dir);
                if (hitAire  == null) hitAire  = TrySphereCastEyesAire(ray.origin, dir);

                if (hitEnemy != null && hitAire != null) break;
            }
        }

        // Si los enemigos no pueden ser stuneados (cooldown o ya stuneados), ignorarlos
        // Así el flag _stunDetected se resetea y queda listo para el siguiente stun
        if (hitEnemy != null && !hitEnemy.CanBeStunned) hitEnemy = null;
        if (hitAire != null && !hitAire.CanBeStunned) hitAire = null;

        bool anyHit = hitEnemy != null || hitAire != null;

        if (anyHit && !_stunDetected)
        {
            _stunDetected = true;

            if (hitEnemy != null)
            {
                hitEnemy.Stun();
                Debug.Log($"[Flashlight] ¡Stun! → {hitEnemy.gameObject.name}");
            }

            if (hitAire != null)
            {
                hitAire.AplicarStun(hitAire.duracionDelStun);
                Debug.Log($"[Flashlight] ¡Stun! → {hitAire.gameObject.name}");
            }
        }
        else if (!anyHit)
        {
            _stunDetected = false;
        }
    }

    private EnemyBehaviourTree TrySphereCastEyes(Vector3 origin, Vector3 direction)
    {
        // SphereCast es como un Raycast pero con un radio (más permisivo)
        RaycastHit[] hits = Physics.SphereCastAll(origin, stunSphereRadius, direction,
            stunDetectRange, ~0, QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            if (!hit.collider.CompareTag(eyesTag)) continue;

            EnemyBehaviourTree bt = hit.collider.GetComponentInParent<EnemyBehaviourTree>();
            if (bt != null) return bt;

            // Si los ojos están en un GameObject separado del Logic, buscar el más cercano
            EnemyBehaviourTree[] allEnemies = Object.FindObjectsByType<EnemyBehaviourTree>(FindObjectsSortMode.None);
            EnemyBehaviourTree nearest = null;
            float minDist = float.MaxValue;
            foreach (var enemy in allEnemies)
            {
                float d = Vector3.Distance(enemy.transform.position, hit.point);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = enemy;
                }
            }
            if (nearest != null) return nearest;
        }

        return null;
    }

    private enemigoaire TrySphereCastEyesAire(Vector3 origin, Vector3 direction)
    {
        int layerMask = ~LayerMask.GetMask("EnemyRange");

        RaycastHit[] hits = Physics.SphereCastAll(origin, stunSphereRadius, direction,
            stunDetectRange, layerMask, QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            if (!hit.collider.CompareTag(eyesTag)) continue;

            enemigoaire bt = hit.collider.GetComponentInParent<enemigoaire>();
            if (bt != null) return bt;

            enemigoaire[] allEnemies = Object.FindObjectsByType<enemigoaire>(FindObjectsSortMode.None);
            enemigoaire nearest = null;
            float minDist = float.MaxValue;
            foreach (var enemy in allEnemies)
            {
                float d = Vector3.Distance(enemy.transform.position, hit.point);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = enemy;
                }
            }
            if (nearest != null) return nearest;
        }

        return null;
    }

    public void Reload()
    {
        if (currentBattery >= maxBattery) return;
        currentBattery = maxBattery;
        OnBatteryChanged?.Invoke(1f);
        if (!_isOn && _hasFlashlight) SetLight(true);
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

    public float BatteryPercent => currentBattery / maxBattery;
    public bool  IsOn           => _isOn;
    public bool  IsAiming       => _isAiming;
    public bool  HasFlashlight  => _hasFlashlight;

    private void OnDrawGizmosSelected()
    {
        if (!startPickedUp)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }
    }

    public void TriggerReloadAnimation()
    {
        if (PlayerEquipmentManager.Instance == null
            || !PlayerEquipmentManager.Instance.IsFlashlightInHand)
            return;

        if (PlayerStateMachine.Instance == null
            || PlayerStateMachine.Instance.BatteryCount <= 0)
        {
            Debug.Log("[Flashlight] Sin baterías, no se puede recargar.");
            return;
        }

        if (currentBattery >= maxBattery)
        {
            Debug.Log("[Flashlight] Batería al máximo.");
            return;
        }

        if (PlayerStateMachine.Instance.playerAnimator == null)
        {
            PlayerStateMachine.Instance.TryReloadFlashlight();
            return;
        }

        Animator anim = PlayerStateMachine.Instance.playerAnimator;
        bool isCrouched = PlayerStateMachine.Instance.IsCrouched;

        if (isCrouched)
        {
            anim.SetTrigger("ReloadCrouched");
            PlayerStateMachine.Instance.EnterTemporaryState(
                PlayerStateMachine.PlayerState.ReloadingCrouched, 1.5f);
        }
        else
        {
            anim.SetTrigger("Reload");
            PlayerStateMachine.Instance.EnterTemporaryState(
                PlayerStateMachine.PlayerState.Reloading, 1.5f);
        }
    }
}