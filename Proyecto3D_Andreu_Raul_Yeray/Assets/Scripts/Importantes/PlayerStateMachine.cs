using UnityEngine;
using StarterAssets;

/// <summary>
/// Máquina de estados central del jugador.
/// Gestiona estados + vida + stamina.
///
/// Coloca en PlayerArmature.
/// </summary>
public class PlayerStateMachine : MonoBehaviour
{
    public static PlayerStateMachine Instance { get; private set; }

    // ─── Estados ─────────────────────────────────────────────────────────────

    public enum PlayerState
    {
        Idle,
        Walking,
        Running,
        AimingFlashlight,
        AimingMegaphone,
        HoldingObject,
        Hiding,
        Dead
    }

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

    // ─── Referencias ─────────────────────────────────────────────────────────

    [Header("Sistemas")]
    public FlashlightSystem       flashlightSystem;
    public LockerSystem           lockerSystem;
    public PlayerEquipmentManager equipmentManager;

    [Header("Starter Assets")]
    public ThirdPersonController tpController;
    public StarterAssetsInputs   inputs;
    public CharacterController   characterController;

    // ─── Inventario de baterías ───────────────────────────────────────────────

    [Header("Inventario — Baterías")]
    public int startingBatteries = 2;
    public int maxBatteries      = 5;
    [Header("Inventario — Baterías (en tiempo real)")]
    [SerializeField] private int _batteryCount;
    public int BatteryCount => _batteryCount;
    // ─── Vida ────────────────────────────────────────────────────────────────

    [Header("Vida")]
    public float maxHealth     = 100f;
    public float currentHealth = 100f;
    [Tooltip("Velocidad de regeneración (por segundo) si no recibe daño durante un tiempo")]
    public float healthRegenRate    = 2f;
    [Tooltip("Segundos sin recibir daño antes de empezar a regenerar")]
    public float healthRegenDelay   = 5f;
    private float _lastDamageTime = -999f;

    // ─── Stamina ─────────────────────────────────────────────────────────────

    [Header("Stamina")]
    public float maxStamina     = 100f;
    public float currentStamina = 100f;
    [Tooltip("Consumo por segundo al correr")]
    public float staminaDrainRate  = 15f;
    [Tooltip("Regeneración por segundo cuando no corre")]
    public float staminaRegenRate  = 10f;
    [Tooltip("Stamina mínima necesaria para empezar a correr")]
    public float staminaRunThreshold = 10f;
    private bool _staminaExhausted = false;

    // ─── Eventos ─────────────────────────────────────────────────────────────

    public System.Action<PlayerState> OnStateChanged;
    public System.Action<int>         OnBatteryCountChanged;
    public System.Action<float>       OnHealthChanged;     // 0-1 (porcentaje)
    public System.Action<float>       OnStaminaChanged;    // 0-1 (porcentaje)
    public System.Action              OnPlayerDied;

    // ─── Estado interno ───────────────────────────────────────────────────────

    private float _originalRunSpeed;

    // ─── Init ─────────────────────────────────────────────────────────────────

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

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        if (CurrentState == PlayerState.Dead) return;

        EvaluateState();
        ApplyStateRules();
        UpdateStamina();
        UpdateHealthRegen();
    }

    // ─── Evaluación de estado ─────────────────────────────────────────────────

    private void EvaluateState()
    {
        // Prioridad: Dead > Hiding > Aiming > HoldingObject > movimiento

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

        // Movimiento
        if (inputs != null)
        {
            float speed = new Vector2(inputs.move.x, inputs.move.y).magnitude;

            // Solo puede correr si tiene stamina
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

    // ─── Reglas por estado ────────────────────────────────────────────────────

    private void ApplyStateRules()
    {
        switch (CurrentState)
        {
            case PlayerState.AimingFlashlight:
            case PlayerState.AimingMegaphone:
                BlockSprint();
                break;

            case PlayerState.HoldingObject:
                if (tpController != null)
                    tpController.SprintSpeed = _originalRunSpeed * 0.7f;
                break;

            case PlayerState.Hiding:
                break;

            case PlayerState.Running:
            case PlayerState.Walking:
            case PlayerState.Idle:
                RestoreSprint();
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

    // ─── Stamina ─────────────────────────────────────────────────────────────

    private void UpdateStamina()
    {
        if (CurrentState == PlayerState.Running)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina  = Mathf.Max(currentStamina, 0f);

            // Si se agota → bloquear sprint hasta llegar al umbral mínimo
            if (currentStamina <= 0f)
            {
                _staminaExhausted = true;
                if (inputs != null) inputs.sprint = false;
            }
        }
        else
        {
            // Regenerar mientras no corre
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina  = Mathf.Min(currentStamina, maxStamina);

            // Cuando vuelve a tener stamina suficiente, deja correr otra vez
            if (_staminaExhausted && currentStamina >= staminaRunThreshold)
                _staminaExhausted = false;
        }

        OnStaminaChanged?.Invoke(currentStamina / maxStamina);
    }

    // ─── Vida ────────────────────────────────────────────────────────────────

    private void UpdateHealthRegen()
    {
        if (currentHealth >= maxHealth) return;

        if (Time.time - _lastDamageTime < healthRegenDelay) return;

        currentHealth += healthRegenRate * Time.deltaTime;
        currentHealth  = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    /// <summary>
    /// Llamado cuando el jugador recibe daño (del enemigo, caída, etc.)
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (CurrentState == PlayerState.Dead) return;

        currentHealth -= amount;
        currentHealth  = Mathf.Max(currentHealth, 0f);
        _lastDamageTime = Time.time;

        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        Debug.Log($"[Health] -{amount}. Vida: {currentHealth:F0}/{maxHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>Cura al jugador (botiquines, checkpoints, etc.)</summary>
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth  = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    // ─── Baterías ────────────────────────────────────────────────────────────

    public bool AddBattery(int amount = 1)
    {
        if (_batteryCount >= maxBatteries) return false;
        _batteryCount = Mathf.Min(_batteryCount + amount, maxBatteries);
        OnBatteryCountChanged?.Invoke(_batteryCount);
        return true;
    }

    public bool TryReloadFlashlight()
    {
        if (_batteryCount <= 0) return false;
        if (flashlightSystem == null) return false;

        _batteryCount--;
        flashlightSystem.Reload();
        OnBatteryCountChanged?.Invoke(_batteryCount);
        return true;
    }

    public void PickupBattery() => AddBattery(1);

    // ─── Muerte ──────────────────────────────────────────────────────────────

    public void Die()
    {
        if (CurrentState == PlayerState.Dead) return;

        ChangeState(PlayerState.Dead);

        if (tpController != null) tpController.enabled = false;
        if (inputs != null)
        {
            inputs.move   = Vector2.zero;
            inputs.look   = Vector2.zero;
            inputs.sprint = false;
        }

        if (flashlightSystem != null && flashlightSystem.IsOn)
            flashlightSystem.ToggleFlashlight();

        OnPlayerDied?.Invoke();
        Debug.Log("[PlayerState] MUERTO");
    }

    // ─── Cambio de estado ────────────────────────────────────────────────────

    private void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    // ─── Propiedades ─────────────────────────────────────────────────────────

    public float HealthPercent  => currentHealth / maxHealth;
    public float StaminaPercent => currentStamina / maxStamina;
    public bool  IsAlive            => CurrentState != PlayerState.Dead;
    public bool  IsHiding           => CurrentState == PlayerState.Hiding;
    public bool  IsAimingFlashlight => CurrentState == PlayerState.AimingFlashlight;
    public bool  IsAimingMegaphone  => CurrentState == PlayerState.AimingMegaphone;
    public bool  IsAiming           => IsAimingFlashlight || IsAimingMegaphone;
    public bool  IsHoldingObject    => CurrentState == PlayerState.HoldingObject;
    public bool  CanRun             => !IsAiming && !IsHiding && !_staminaExhausted;
    public bool  HasBattery         => _batteryCount > 0;
}