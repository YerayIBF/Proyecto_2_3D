using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

/// <summary>
/// Máquina de estados central del jugador.
/// Gestiona estados + vida + stamina + agachado + animación de daño.
/// </summary>
public class PlayerStateMachine : MonoBehaviour
{
    public static PlayerStateMachine Instance { get; private set; }

    public enum PlayerState
    {
        Idle, Walking, Running, Crouched,
        AimingFlashlight, AimingMegaphone, HoldingObject,
        Picking, Throwing, ThrowingCrouched, Reloading, ReloadingCrouched,
        TakingDamage, Hiding, Dead
    }

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

    [Header("Sistemas")]
    public FlashlightSystem       flashlightSystem;
    public LockerSystem           lockerSystem;
    public PlayerEquipmentManager equipmentManager;

    [Header("Starter Assets")]
    public ThirdPersonController tpController;
    public StarterAssetsInputs   inputs;
    public CharacterController   characterController;

    [Header("Animator del personaje")]
    public Animator playerAnimator;

    [Header("Inventario — Baterías (linterna)")]
    public int startingBatteries = 2;
    public int maxBatteries      = 5;
    [SerializeField] private int _batteryCount;
    public int BatteryCount => _batteryCount;

    [Header("Pilas del megáfono")]
    public int megafonoBatteryCount = 0;
    public int maxMegafonoBatteries = 5;
    public int MegafonoBatteryCount => megafonoBatteryCount;

    [Header("Vida")]
    public float maxHealth     = 100f;
    public float currentHealth = 100f;
    public float healthRegenRate  = 2f;
    public float healthRegenDelay = 5f;
    private float _lastDamageTime = -999f;

    [Header("Stamina")]
    public float maxStamina     = 100f;
    public float currentStamina = 100f;
    public float staminaDrainRate    = 15f;
    public float staminaRegenRate    = 10f;
    public float staminaRunThreshold = 10f;
    private bool _staminaExhausted = false;

    [Header("Audio — Agotamiento stamina")]
    public AudioSource audioAgotamiento;
    public AudioClip   sonidoAgotamiento;

    [Header("Audio — Latidos de corazón")]
    public AudioSource audioLatidos;
    [Tooltip("Vida a la que empiezan a oírse los latidos")]
    public float vidaInicioLatidos = 80f;
    [Tooltip("Vida a la que los latidos van al máximo de velocidad")]
    public float vidaLatidosMaximos = 30f;
    [Tooltip("Pitch mínimo de los latidos al empezar")]
    public float pitchLatidosMin = 0.7f;
    [Tooltip("Pitch máximo de los latidos (no pasar de aquí)")]
    public float pitchLatidosMax = 1f;

    [Header("Agacharse")]
    public KeyCode crouchKey       = KeyCode.C;
    public float   crouchMoveSpeed = 1.5f;
    public float   standMoveSpeed  = 2.0f;
    public float   standSprintSpeed = 5.335f;
    public float   standHeight    = 1.8f;
    public float   crouchHeight   = 1f;
    public float   crouchCenterY  = 0.5f;
    public float   standCenterY   = 0.9f;

    [Header("Crouch — Input System")]
    public InputActionReference crouchAction;

    private bool _isCrouched = false;
    public bool IsCrouched => _isCrouched;

    private PlayerState _temporaryState = PlayerState.Idle;
    private float _temporaryStateEndTime = -1f;
    private bool _inTemporaryState = false;

    public System.Action<PlayerState> OnStateChanged;
    public System.Action<int>         OnBatteryCountChanged;
    public System.Action<float>       OnHealthChanged;
    public System.Action<float>       OnStaminaChanged;
    public System.Action<bool>        OnCrouchChanged;
    public System.Action              OnPlayerDied;

    private static readonly int HashIsCrouched  = Animator.StringToHash("IsCrouched");
    private static readonly int HashCrouchSpeed = Animator.StringToHash("CrouchSpeed");
    private static readonly int HashTakeDamage  = Animator.StringToHash("TakeDamage");
    private static readonly int HashIsDead      = Animator.StringToHash("IsDead");

    private float _originalRunSpeed;

    private void OnEnable()
    {
        if (crouchAction != null) crouchAction.action.Enable();
    }

    private void OnDisable()
    {
        if (crouchAction != null) crouchAction.action.Disable();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (tpController        == null) tpController        = GetComponent<ThirdPersonController>();
        if (inputs              == null) inputs              = GetComponent<StarterAssetsInputs>();
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (lockerSystem        == null) lockerSystem        = GetComponent<LockerSystem>();
        if (equipmentManager    == null) equipmentManager    = GetComponent<PlayerEquipmentManager>();
        if (flashlightSystem    == null) flashlightSystem    = GetComponentInChildren<FlashlightSystem>();
        if (playerAnimator      == null) playerAnimator      = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        _batteryCount  = startingBatteries;
        currentHealth  = maxHealth;
        currentStamina = maxStamina;

        if (tpController != null)
            _originalRunSpeed = tpController.SprintSpeed;

        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        OnStaminaChanged?.Invoke(currentStamina / maxStamina);
    }

    private void Update()
    {
        if (CurrentState == PlayerState.Dead) { DetenerLatidos(); return; }

        HandleCrouchInput();
        EvaluateState();
        ApplyStateRules();
        UpdateStamina();
        UpdateHealthRegen();
        UpdateCrouchAnimation();
        UpdateLatidos();
    }

    private void HandleCrouchInput()
    {
        bool crouchInput = Input.GetKeyDown(crouchKey)
                        || (crouchAction != null && crouchAction.action.WasPressedThisFrame());

        if (crouchInput)
            ToggleCrouch();
    }

    public void ToggleCrouch()
    {
        if (IsAiming || IsHiding || CurrentState == PlayerState.Dead) return;

        _isCrouched = !_isCrouched;
        ApplyCrouchState();
        OnCrouchChanged?.Invoke(_isCrouched);
    }

    private void ApplyCrouchState()
    {
        if (playerAnimator != null)
            playerAnimator.SetBool(HashIsCrouched, _isCrouched);

        if (tpController != null)
        {
            if (_isCrouched)
            {
                tpController.MoveSpeed   = crouchMoveSpeed;
                tpController.SprintSpeed = crouchMoveSpeed;
            }
            else
            {
                tpController.MoveSpeed   = standMoveSpeed;
                tpController.SprintSpeed = standSprintSpeed;
                _originalRunSpeed = standSprintSpeed;
            }
        }

        if (characterController != null)
        {
            if (_isCrouched)
            {
                characterController.height = crouchHeight;
                characterController.center = new Vector3(0f, crouchCenterY, 0f);
            }
            else
            {
                characterController.height = standHeight;
                characterController.center = new Vector3(0f, standCenterY, 0f);
            }
        }

        Debug.Log($"[State] {(_isCrouched ? "Agachado" : "De pie")}");
    }

    private void UpdateCrouchAnimation()
    {
        if (!_isCrouched || playerAnimator == null || inputs == null) return;

        float moveMagnitude = new Vector2(inputs.move.x, inputs.move.y).magnitude;
        playerAnimator.SetFloat(HashCrouchSpeed, moveMagnitude, 0.1f, Time.deltaTime);
    }

    public void ForceStand()
    {
        if (_isCrouched)
        {
            _isCrouched = false;
            ApplyCrouchState();
            OnCrouchChanged?.Invoke(false);
        }
    }

    private void EvaluateState()
    {
        if (_inTemporaryState)
        {
            if (Time.time >= _temporaryStateEndTime)
            {
                _inTemporaryState = false;
            }
            else
            {
                ChangeState(_temporaryState);
                return;
            }
        }

        if (lockerSystem != null && lockerSystem.IsHiding)
        {
            ChangeState(PlayerState.Hiding);
            return;
        }

        if (equipmentManager != null)
        {
            if (equipmentManager.IsAimingFlashlight) { ChangeState(PlayerState.AimingFlashlight); return; }
            if (equipmentManager.IsAimingMegaphone)  { ChangeState(PlayerState.AimingMegaphone);  return; }
            if (equipmentManager.IsHoldingThrowable) { ChangeState(PlayerState.HoldingObject);    return; }
        }

        if (_isCrouched)
        {
            ChangeState(PlayerState.Crouched);
            return;
        }

        if (inputs != null)
        {
            float speed = new Vector2(inputs.move.x, inputs.move.y).magnitude;

            bool wantsToRun = speed > 0.1f && inputs.sprint;
            bool canRun     = !_staminaExhausted && currentStamina > 0f;

            if (wantsToRun && canRun)
                ChangeState(PlayerState.Running);
            else if (speed > 0.1f)
                ChangeState(PlayerState.Walking);
            else
                ChangeState(PlayerState.Idle);
        }
    }

    public void EnterTemporaryState(PlayerState state, float duration)
    {
        if (CurrentState == PlayerState.Dead) return;

        _temporaryState = state;
        _temporaryStateEndTime = Time.time + duration;
        _inTemporaryState = true;
        ChangeState(state);
    }

    public void ExitTemporaryState()
    {
        _inTemporaryState = false;
        _temporaryStateEndTime = -1f;
    }

    private void ApplyStateRules()
    {
        switch (CurrentState)
        {
            case PlayerState.AimingFlashlight:
            case PlayerState.AimingMegaphone:
                BlockSprint();
                break;

            case PlayerState.HoldingObject:
                if (tpController != null && !_isCrouched)
                    tpController.SprintSpeed = _originalRunSpeed * 0.7f;
                break;

            case PlayerState.Hiding:
                break;

            case PlayerState.Crouched:
                break;

            case PlayerState.Running:
            case PlayerState.Walking:
            case PlayerState.Idle:
                if (!_isCrouched) RestoreSprint();
                break;
        }
    }

    private void BlockSprint()
    {
        if (inputs != null) inputs.sprint = false;
        if (tpController != null) tpController.SprintSpeed = tpController.MoveSpeed;
    }

    private void RestoreSprint()
    {
        if (tpController != null) tpController.SprintSpeed = _originalRunSpeed;
    }

    private void UpdateStamina()
    {
        if (CurrentState == PlayerState.Running)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina  = Mathf.Max(currentStamina, 0f);

            if (currentStamina <= 0f)
            {
                // Sonido de agotamiento solo en el momento de agotarse
                if (!_staminaExhausted)
                {
                    if (audioAgotamiento != null && sonidoAgotamiento != null)
                        audioAgotamiento.PlayOneShot(sonidoAgotamiento);
                }

                _staminaExhausted = true;
                if (inputs != null) inputs.sprint = false;
            }
        }
        else
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina  = Mathf.Min(currentStamina, maxStamina);

            if (_staminaExhausted && currentStamina >= staminaRunThreshold)
                _staminaExhausted = false;
        }

        OnStaminaChanged?.Invoke(currentStamina / maxStamina);
    }

    private void UpdateHealthRegen()
    {
        if (currentHealth >= maxHealth) return;
        if (Time.time - _lastDamageTime < healthRegenDelay) return;

        currentHealth += healthRegenRate * Time.deltaTime;
        currentHealth  = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (CurrentState == PlayerState.Dead) return;

        currentHealth -= amount;
        currentHealth  = Mathf.Max(currentHealth, 0f);
        _lastDamageTime = Time.time;

        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        Debug.Log($"[Health] -{amount}. Vida: {currentHealth:F0}/{maxHealth}");

        if (currentHealth > 0f && playerAnimator != null)
        {
            playerAnimator.SetTrigger(HashTakeDamage);
            EnterTemporaryState(PlayerState.TakingDamage, 0.5f);
        }

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth  = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    public bool AddBattery(int amount = 1)
    {
        if (_batteryCount >= maxBatteries) return false;
        _batteryCount = Mathf.Min(_batteryCount + amount, maxBatteries);
        OnBatteryCountChanged?.Invoke(_batteryCount);
        Debug.Log($"[Inventory] +{amount} batería linterna. Total: {_batteryCount}/{maxBatteries}");
        return true;
    }

    public bool TryReloadFlashlight()
    {
        if (_batteryCount <= 0)
        {
            Debug.Log("[Inventory] Sin baterías para recargar.");
            return false;
        }
        if (flashlightSystem == null) return false;

        _batteryCount--;
        flashlightSystem.Reload();
        OnBatteryCountChanged?.Invoke(_batteryCount);
        Debug.Log($"[Inventory] Batería usada. Quedan: {_batteryCount}/{maxBatteries}");
        return true;
    }

    public void PickupBattery() => AddBattery(1);

    public void AddMegafonoBattery(int amount = 1)
    {
        megafonoBatteryCount = Mathf.Min(megafonoBatteryCount + amount, maxMegafonoBatteries);
        Debug.Log($"[Inventory] +{amount} pila megáfono. Total: {megafonoBatteryCount}/{maxMegafonoBatteries}");
    }

    public bool TryReloadMegafono()
    {
        if (megafonoBatteryCount <= 0)
        {
            Debug.Log("[Inventory] Sin pilas de megáfono.");
            return false;
        }

        Megafono megafono = Object.FindFirstObjectByType<Megafono>();
        if (megafono == null) return false;

        megafono.RecargarEnergia(megafono.maxEnergia);
        megafonoBatteryCount--;
        Debug.Log($"[Inventory] Pila megáfono usada. Quedan: {megafonoBatteryCount}/{maxMegafonoBatteries}");
        return true;
    }

    public void Die()
    {
        if (CurrentState == PlayerState.Dead) return;

        ChangeState(PlayerState.Dead);

        if (lockerSystem != null && lockerSystem.IsHiding)
            lockerSystem.ForceExitOnDeath();

        if (tpController != null) tpController.enabled = false;
        if (inputs != null)
        {
            inputs.move   = Vector2.zero;
            inputs.look   = Vector2.zero;
            inputs.sprint = false;
            inputs.jump   = false;
        }

        if (_isCrouched)
        {
            _isCrouched = false;
            if (characterController != null)
            {
                characterController.height = standHeight;
                characterController.center = new Vector3(0f, standCenterY, 0f);
            }
        }

        if (playerAnimator != null)
            playerAnimator.SetBool(HashIsDead, true);

        if (flashlightSystem != null && flashlightSystem.IsOn)
            flashlightSystem.ToggleFlashlight();

        DetenerLatidos();

        OnPlayerDied?.Invoke();
        Debug.Log("[PlayerState] MUERTO");
    }

    public void Respawn(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        Debug.Log("[PlayerState] Respawn...");

        currentHealth = maxHealth;
        currentStamina = maxStamina;
        _staminaExhausted = false;
        _lastDamageTime = -999f;
        _inTemporaryState = false;

        if (playerAnimator != null)
        {
            playerAnimator.SetBool(HashIsDead, false);
            playerAnimator.enabled = true;
        }

        PlayerRagdoll ragdoll = GetComponent<PlayerRagdoll>();
        if (ragdoll != null) ragdoll.DeactivateRagdoll();

        DeathCamera deathCam = FindFirstObjectByType<DeathCamera>();
        if (deathCam != null) deathCam.ResetCamera();

        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            characterController.enabled = true;
        }
        else
        {
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
        }

        if (tpController != null) tpController.enabled = true;

        ChangeState(PlayerState.Idle);

        OnHealthChanged?.Invoke(1f);
        OnStaminaChanged?.Invoke(1f);
    }

    private void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    // ─── Latidos de corazón según la vida ──────────────────────────────────

    private void UpdateLatidos()
    {
        if (audioLatidos == null) return;

        // Por encima de la vida de inicio: sin latidos
        if (currentHealth > vidaInicioLatidos)
        {
            if (audioLatidos.isPlaying) audioLatidos.Stop();
            return;
        }

        // Dentro del rango: arrancar si no suena
        if (!audioLatidos.isPlaying)
        {
            audioLatidos.loop = true;
            audioLatidos.Play();
        }

        // t=0 en vidaInicioLatidos, t=1 en vidaLatidosMaximos (clamp para no pasar de pitchMax)
        float t = Mathf.InverseLerp(vidaInicioLatidos, vidaLatidosMaximos, currentHealth);
        audioLatidos.pitch = Mathf.Lerp(pitchLatidosMin, pitchLatidosMax, t);
    }

    private void DetenerLatidos()
    {
        if (audioLatidos != null && audioLatidos.isPlaying)
            audioLatidos.Stop();
    }

    public float HealthPercent  => currentHealth / maxHealth;
    public float StaminaPercent => currentStamina / maxStamina;
    public bool  IsAlive            => CurrentState != PlayerState.Dead;
    public bool  IsHiding           => CurrentState == PlayerState.Hiding;
    public bool  IsAimingFlashlight => CurrentState == PlayerState.AimingFlashlight;
    public bool  IsAimingMegaphone  => CurrentState == PlayerState.AimingMegaphone;
    public bool  IsAiming           => IsAimingFlashlight || IsAimingMegaphone;
    public bool  IsHoldingObject    => CurrentState == PlayerState.HoldingObject;
    public bool  CanRun             => !IsAiming && !IsHiding && !_staminaExhausted && !_isCrouched;
    public bool  HasBattery         => _batteryCount > 0;
}