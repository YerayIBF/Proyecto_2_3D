using UnityEngine;

/// <summary>
/// Emite ruido periódico al sistema de detección de enemigos
/// según el estado de movimiento del jugador.
///
/// Reglas:
///   Agachado     → sin ruido
///   Caminando    → ruido moderado  (radioBase)
///   Corriendo    → ruido alto      (radioBase * runMultiplier)
///   Quieto       → sin ruido
/// </summary>
[RequireComponent(typeof(PlayerStateMachine))]
public class PlayerSoundEmitter : MonoBehaviour
{
    // ─── Configuración ────────────────────────────────────────────────────────

    [Header("Radios de emisión de sonido")]
    [Tooltip("Radio base al caminar (metros)")]
    public float walkRadius = 8f;

    [Tooltip("Multiplicador sobre walkRadius al correr")]
    public float runMultiplier = 2.5f;

    [Tooltip("Intervalo en segundos entre emisiones de sonido al caminar")]
    public float walkInterval = 0.4f;

    [Tooltip("Intervalo en segundos entre emisiones de sonido al correr")]
    public float runInterval = 0.2f;

    // ─── Debug ────────────────────────────────────────────────────────────────

    [Header("Debug")]
    [Tooltip("Mostrar esfera de sonido en Scene View")]
    public bool drawGizmos = true;

    // ─── Estado interno ───────────────────────────────────────────────────────

    private PlayerStateMachine _psm;
    private float _timer;
    private float _lastEmittedRadius;   // solo para Gizmos

    // ─── Init ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _psm = GetComponent<PlayerStateMachine>();
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_psm.IsAlive) return;

        GetEmissionParams(out float radius, out float interval);

        // Sin radio → no emite, resetea el timer
        if (radius <= 0f)
        {
            _timer = 0f;
            return;
        }

        _timer += Time.deltaTime;

        if (_timer >= interval)
        {
            _timer = 0f;
            EmitNoise(radius);
        }
    }

    // ─── Lógica central ───────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve el radio y el intervalo según el estado actual del jugador.
    /// Radio 0 significa "no emitir".
    /// </summary>
    private void GetEmissionParams(out float radius, out float interval)
    {
        PlayerStateMachine.PlayerState state = _psm.CurrentState;

        switch (state)
        {
            case PlayerStateMachine.PlayerState.Running:
                radius   = walkRadius * runMultiplier;
                interval = runInterval;
                break;

            case PlayerStateMachine.PlayerState.Walking:
                radius   = walkRadius;
                interval = walkInterval;
                break;

            // Agachado, quieto, apuntando sin moverse, escondido, muerto → silencio
            default:
                radius   = 0f;
                interval = walkInterval;
                break;
        }
    }

    private void EmitNoise(float radius)
    {
        _lastEmittedRadius = radius;

        if (EmitirSonido.instance != null)
            EmitirSonido.instance.EmitirRuido(transform.position, radius);
        else
            Debug.LogWarning("[PlayerSoundEmitter] EmitirSonido.instance es null.");
    }

    // ─── Gizmos ───────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        // Radio al caminar (verde)
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, walkRadius);

        // Radio al correr (naranja)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, walkRadius * runMultiplier);

        // Última emisión real (blanco, solo en Play)
        if (Application.isPlaying && _lastEmittedRadius > 0f)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, _lastEmittedRadius);
        }
    }
}