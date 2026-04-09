using UnityEngine;
using System.Collections;

/// <summary>
/// Coloca en el GameObject del enemigo.
/// Se suscribe al NoiseEmitter y cuando oye un ruido dentro de su rango,
/// calcula una posición APROXIMADA (no exacta) para investigar.
///
/// La imprecisión depende de la intensidad del ruido:
///   Intensidad 1.0 → desvío máximo 0m   (localiza perfectamente)
///   Intensidad 0.5 → desvío máximo 2.5m
///   Intensidad 0.0 → desvío máximo 5m   (solo sabe la zona general)
/// </summary>
public class NoiseSensor : MonoBehaviour
{
    [Header("Detección")]
    [Tooltip("Radio máximo al que el enemigo puede oír ruidos")]
    public float hearingRange = 15f;

    [Tooltip("Desvío máximo en metros cuando la intensidad es mínima")]
    public float maxImprecision = 5f;

    [Header("Cooldown")]
    [Tooltip("Segundos de espera entre reacciones a ruidos consecutivos")]
    public float reactionCooldown = 2f;

    // Estado público — el Behaviour Tree leerá estas propiedades
    public bool  HeardNoise           { get; private set; } = false;
    public Vector3 NoisePosition      { get; private set; } = Vector3.zero;
    public float   NoiseIntensity     { get; private set; } = 0f;

    private float _cooldownTimer = 0f;
    private bool  _onCooldown    = false;

    // ─── Enable / Disable ────────────────────────────────────────────────────

    private void OnEnable()
    {
        NoiseEmitter.OnNoiseEmitted += OnNoiseReceived;
    }

    private void OnDisable()
    {
        NoiseEmitter.OnNoiseEmitted -= OnNoiseReceived;
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        // Gestionar cooldown
        if (_onCooldown)
        {
            _cooldownTimer += Time.deltaTime;
            if (_cooldownTimer >= reactionCooldown)
            {
                _onCooldown    = false;
                _cooldownTimer = 0f;
            }
        }

        // El Behaviour Tree resetea HeardNoise una vez lo ha procesado
        // llamando a NoiseSensor.AcknowledgeNoise()
    }

    // ─── Callback del NoiseEmitter ───────────────────────────────────────────

    private void OnNoiseReceived(Vector3 noisePosition, float noiseRadius, float intensity)
    {
        if (_onCooldown) return;

        // Comprobar si el ruido está dentro del rango de escucha del enemigo
        float distToNoise = Vector3.Distance(transform.position, noisePosition);
        if (distToNoise > hearingRange) return;
        if (distToNoise > noiseRadius)  return;

        // Calcular posición aproximada según intensidad
        // Baja intensidad = mayor imprecisión
        float imprecision = maxImprecision * (1f - intensity);
        Vector3 approximatePosition = noisePosition + Random.insideUnitSphere * imprecision;
        approximatePosition.y = noisePosition.y; // mantener altura original

        HeardNoise     = true;
        NoisePosition  = approximatePosition;
        NoiseIntensity = intensity;

        _onCooldown    = true;
        _cooldownTimer = 0f;

        Debug.Log($"[NoiseSensor] Enemigo oyó ruido a {distToNoise:F1}m | " +
                  $"Intensidad: {intensity:F2} | Imprecisión: {imprecision:F1}m");
    }

    /// <summary>
    /// El Behaviour Tree llama esto cuando ya ha procesado el ruido.
    /// </summary>
    public void AcknowledgeNoise()
    {
        HeardNoise = false;
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        // Radio de escucha del enemigo (azul)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        // Posición aproximada de ruido detectado (rojo)
        if (HeardNoise)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(NoisePosition, 0.4f);
            Gizmos.DrawLine(transform.position, NoisePosition);
        }
    }
}
