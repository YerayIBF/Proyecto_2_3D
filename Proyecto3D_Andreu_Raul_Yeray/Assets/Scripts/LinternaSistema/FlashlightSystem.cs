using UnityEngine;
using System.Collections;

/// <summary>
/// Sistema de linterna.
///
/// - El jugador no puede usar la linterna hasta recogerla con E
/// - Si startPickedUp = true, ya la tiene equipada al inicio (test/tutorial saltado)
/// - F = encender/apagar
/// - Click DERECHO = apuntar (zoom + más intensa)
/// - R = recargar
///
/// Para el stun: detecta colliders con el tag "EnemyEyes".
/// </summary>
public class FlashlightSystem : MonoBehaviour
{
    [Header("Pickup")]
    [Tooltip("Si está marcado, la linterna empieza ya recogida (no necesita E)")]
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
    [Tooltip("Tag de los colliders de ojos de los enemigos. Por defecto 'EnemyEyes'.")]
    public string eyesTag = "EnemyEyes";
    public int      coneRayCount    = 8;
    public float    coneAngle       = 15f;
    public float    stunDetectRange = 10f;

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

    // ─── Init ────────────────────────────────────────────────────────────────

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
        if (startPickedUp)
        {
            _hasFlashlight = true;
        }
        else
        {
            _hasFlashlight = false;
        }
    }

    private void Update()
    {

        
        if (PlayerStateMachine.Instance != null && !PlayerStateMachine.Instance.IsAlive)
        return;

        if (!_hasFlashlight)
        {
            HandlePickup();
            return;
        }

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

    // ─── PICKUP ──────────────────────────────────────────────────────────────

    private void HandlePickup()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        bool nowInRange = dist <= pickupRange;

        if (nowInRange != _playerInRange)
        {
            _playerInRange = nowInRange;
            if (pickupIcon != null) pickupIcon.SetActive(_playerInRange);
        }

        if (_playerInRange && Input.GetKeyDown(KeyCode.E))
            Pickup();
    }

    private void Pickup()
    {
        _hasFlashlight = true;
        if (pickupIcon != null) pickupIcon.SetActive(false);

        if (PlayerEquipmentManager.Instance != null)
            PlayerEquipmentManager.Instance.PickupFlashlight(gameObject);

        if (GameManager.instance != null)
            GameManager.instance.tieneLinterna = true;

        Debug.Log("[Flashlight] Linterna recogida.");
    }

    public void PickupFlashlight()
    {
        _hasFlashlight = true;
        if (pickupIcon != null) pickupIcon.SetActive(false);
    }

    // ─── Input ───────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        // Si está escondido en taquilla, apagar y bloquear la linterna
        if (PlayerStateMachine.Instance != null && PlayerStateMachine.Instance.IsHiding)
        {
            if (_isOn) SetLight(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.F) && currentBattery > 0f)
            ToggleFlashlight();

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (PlayerStateMachine.Instance != null)
                PlayerStateMachine.Instance.TryReloadFlashlight();
            else
                Reload();
        }

        bool canAim = _isOn;
        if (PlayerEquipmentManager.Instance != null)
            canAim = canAim && PlayerEquipmentManager.Instance.CanAimFlashlight;

        bool aimInput = Input.GetMouseButton(1) && canAim;

        if (aimInput != _isAiming)
        {
            _isAiming = aimInput;
            OnAimingChanged?.Invoke(_isAiming);
            PlayerEquipmentManager.Instance?.SetAimingFlashlight(_isAiming);

            if (!_isAiming) ResetStunCharge();
        }
}

    // ─── Parpadeo ─────────────────────────────────────────────────────────────

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

    // ─── Luz ─────────────────────────────────────────────────────────────────

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

    // ─── DETECCIÓN OJOS POR TAG ──────────────────────────────────────────────

   private void DetectEyesStun()
{
    if (!_isAiming || mainCamera == null) return;

    Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

    EnemyBehaviourTree hitEnemy = TryRaycastEyes(ray.origin, ray.direction);
    enemigoaire hitAire = TryRaycastEyesAire(ray.origin, ray.direction);

    if (hitEnemy == null && hitAire == null)
    {
        for (int i = 0; i < coneRayCount; i++)
        {
            float angle = (360f / coneRayCount) * i;
            Vector3 dir = Quaternion.AngleAxis(angle, ray.direction)
                        * (Quaternion.AngleAxis(coneAngle * 0.5f, mainCamera.transform.right) * ray.direction);

            if (hitEnemy == null) hitEnemy = TryRaycastEyes(ray.origin, dir);
            if (hitAire  == null) hitAire  = TryRaycastEyesAire(ray.origin, dir);

            if (hitEnemy != null && hitAire != null) break;
        }
    }

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

    

    /// <summary>
    /// Lanza un raycast y comprueba si lo que golpea tiene el tag eyesTag.
    /// Si sí, busca el EnemyBehaviourTree en la escena (porque puede estar en
    /// otro GameObject — el EnemyLogic, separado del Ghost donde están los ojos).
    /// </summary>
    private EnemyBehaviourTree TryRaycastEyes(Vector3 origin, Vector3 direction)
    {
       if (!Physics.Raycast(origin, direction, out RaycastHit hit, stunDetectRange,
        ~0, QueryTriggerInteraction.Collide))
        return null;

        // Comprobar tag
        if (!hit.collider.CompareTag(eyesTag))
            return null;

        // Buscar el BehaviourTree en padres O en toda la escena
        EnemyBehaviourTree bt = hit.collider.GetComponentInParent<EnemyBehaviourTree>();
        if (bt != null) return bt;

        // Si los ojos están en el Ghost (separado del Logic), buscamos al más cercano
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
        return nearest;
    }

    private enemigoaire TryRaycastEyesAire(Vector3 origin, Vector3 direction)
    {
        int layerMask = ~LayerMask.GetMask("EnemyRange"); // ignora el collider grande

       if (!Physics.Raycast(origin, direction, out RaycastHit hit, stunDetectRange,
        layerMask, QueryTriggerInteraction.Collide))
        return null;

        // Comprobar tag
        if (!hit.collider.CompareTag(eyesTag))
        return null;

        // Buscar el BehaviourTree en padres O en toda la escena
        enemigoaire bt = hit.collider.GetComponentInParent<enemigoaire>();
        if (bt != null) return bt;

        // Si los ojos están en el Ghost (separado del Logic), buscamos al más cercano
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
        return nearest;
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
}