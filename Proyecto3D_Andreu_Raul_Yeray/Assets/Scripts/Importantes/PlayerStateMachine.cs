using UnityEngine;
using StarterAssets;

/// <summary>
/// Máquina de estados central del jugador.
/// Lee el estado del PlayerEquipmentManager y otros sistemas, y coordina
/// las restricciones de movimiento, sprint, etc.
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

    [Header("Inventario")]
    public int startingBatteries = 2;
    public int maxBatteries      = 5;

    private int _batteryCount;
    public int BatteryCount => _batteryCount;

    // ─── Eventos ─────────────────────────────────────────────────────────────

    public System.Action<PlayerState> OnStateChanged;
    public System.Action<int>         OnBatteryCountChanged;
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
        _batteryCount = startingBatteries;

        if (tpController != null)
            _originalRunSpeed = tpController.SprintSpeed;
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        if (CurrentState == PlayerState.Dead) return;

        EvaluateState();
        ApplyStateRules();
    }

    // ─── Evaluación de estado ─────────────────────────────────────────────────

    private void EvaluateState()
    {
        // Prioridad: Dead > Hiding > AimingFlashlight > AimingMegaphone > HoldingObject > movimiento

        // Escondido en taquilla
        if (lockerSystem != null && lockerSystem.IsHiding)
        {
            ChangeState(PlayerState.Hiding);
            return;
        }

        // Estados del equipment manager
        if (equipmentManager != null)
        {
            if (equipmentManager.IsAimingFlashlight)
            {
                ChangeState(PlayerState.AimingFlashlight);
                return;
            }
            if (equipmentManager.IsAimingMegaphone)
            {
                ChangeState(PlayerState.AimingMegaphone);
                return;
            }
            if (equipmentManager.IsHoldingThrowable)
            {
                ChangeState(PlayerState.HoldingObject);
                return;
            }
        }

        // Movimiento normal
        if (inputs != null)
        {
            float speed = new Vector2(inputs.move.x, inputs.move.y).magnitude;

            if (speed > 0.1f && inputs.sprint)
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
                // No puede correr mientras apunta cualquier cosa
                BlockSprint();
                break;

            case PlayerState.HoldingObject:
                // Puede correr llevando un objeto pero más lento (opcional)
                if (tpController != null)
                    tpController.SprintSpeed = _originalRunSpeed * 0.7f;
                break;

            case PlayerState.Hiding:
                // Lo gestiona LockerSystem
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

    // ─── Cambio de estado ─────────────────────────────────────────────────────

    private void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
        Debug.Log($"[PlayerState] → {newState}");
    }

    // ─── API pública — Baterías ───────────────────────────────────────────────

    public bool AddBattery(int amount = 1)
    {
        if (_batteryCount >= maxBatteries) return false;

        _batteryCount = Mathf.Min(_batteryCount + amount, maxBatteries);
        OnBatteryCountChanged?.Invoke(_batteryCount);
        Debug.Log($"[Inventory] Baterías: {_batteryCount}/{maxBatteries}");
        return true;
    }

    public bool TryReloadFlashlight()
    {
        if (_batteryCount <= 0)
        {
            Debug.Log("[Inventory] Sin baterías.");
            return false;
        }
        if (flashlightSystem == null) return false;

        _batteryCount--;
        flashlightSystem.Reload();
        OnBatteryCountChanged?.Invoke(_batteryCount);
        Debug.Log($"[Inventory] Batería usada. Quedan: {_batteryCount}");
        return true;
    }

    public void PickupBattery() => AddBattery(1);

    // ─── API pública — Muerte ─────────────────────────────────────────────────

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
        Debug.Log("[PlayerState] JUGADOR MUERTO.");
    }

    // ─── Propiedades públicas ────────────────────────────────────────────────

    public bool IsAlive            => CurrentState != PlayerState.Dead;
    public bool IsHiding           => CurrentState == PlayerState.Hiding;
    public bool IsAimingFlashlight => CurrentState == PlayerState.AimingFlashlight;
    public bool IsAimingMegaphone  => CurrentState == PlayerState.AimingMegaphone;
    public bool IsAiming           => IsAimingFlashlight || IsAimingMegaphone;
    public bool IsHoldingObject    => CurrentState == PlayerState.HoldingObject;
    public bool CanRun             => !IsAiming && !IsHiding;
    public bool HasBattery         => _batteryCount > 0;
}