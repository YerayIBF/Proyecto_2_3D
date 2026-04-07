using UnityEngine;
using StarterAssets;

/// <summary>
/// Coloca en PlayerArmature junto a LockerSystem y PlayerThrowSystem.
/// Emite ruido de pasos automáticamente según el movimiento del jugador.
///
/// Radios orientativos:
///   Corriendo  → 10m
///   Andando    → 5m
///   Agachado   → 2m
///   Escondido  → 0m (silencio total)
/// </summary>
public class PlayerNoiseEmitter : MonoBehaviour
{
    [Header("Radios de ruido")]
    public float runningNoiseRadius  = 10f;
    public float walkingNoiseRadius  = 5f;
    public float crouchingNoiseRadius = 2f;

    [Header("Pasos")]
    [Tooltip("Intervalo en segundos entre emisiones de ruido al andar")]
    public float walkStepInterval   = 0.5f;
    [Tooltip("Intervalo al correr")]
    public float runStepInterval    = 0.3f;
    [Tooltip("Intervalo al agacharse")]
    public float crouchStepInterval = 0.7f;

    [Header("Velocidad mínima para emitir ruido")]
    public float minMoveSpeed = 0.1f;

    // Referencias
    private StarterAssetsInputs  _inputs;
    private CharacterController  _charController;
    private LockerSystem         _lockerSystem;
    private ThirdPersonController _tpController;

    // Estado
    private float _stepTimer = 0f;

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _inputs         = GetComponent<StarterAssetsInputs>();
        _charController = GetComponent<CharacterController>();
        _lockerSystem   = GetComponent<LockerSystem>();
        _tpController   = GetComponent<ThirdPersonController>();
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        // Silencio total si está escondido en una taquilla
        if (_lockerSystem != null && _lockerSystem.IsHiding) return;

        // Velocidad horizontal del jugador
        Vector3 horizontalVelocity = new Vector3(
            _charController.velocity.x, 0f, _charController.velocity.z);
        float speed = horizontalVelocity.magnitude;

        // Sin movimiento = sin ruido
        if (speed < minMoveSpeed) { _stepTimer = 0f; return; }

        bool isCrouching = _inputs != null && _inputs.analogMovement == false
                           && speed < 1.5f; // heurística: lento = agachado
        bool isSprinting = _inputs != null && _inputs.sprint;

        // Seleccionar radio e intervalo según estado
        float noiseRadius;
        float stepInterval;
        float intensity;

        if (isCrouching)
        {
            noiseRadius    = crouchingNoiseRadius;
            stepInterval   = crouchStepInterval;
            intensity      = 0.2f;
        }
        else if (isSprinting)
        {
            noiseRadius    = runningNoiseRadius;
            stepInterval   = runStepInterval;
            intensity      = 1f;
        }
        else
        {
            noiseRadius    = walkingNoiseRadius;
            stepInterval   = walkStepInterval;
            intensity      = 0.5f;
        }

        // Emitir ruido cada X segundos mientras se mueve
        _stepTimer += Time.deltaTime;
        if (_stepTimer >= stepInterval)
        {
            _stepTimer = 0f;
            NoiseEmitter.Emit(transform.position, noiseRadius, intensity);
        }
    }
}
