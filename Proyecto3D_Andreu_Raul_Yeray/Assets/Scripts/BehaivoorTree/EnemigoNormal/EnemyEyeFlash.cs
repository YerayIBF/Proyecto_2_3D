using UnityEngine;
using System.Collections;

/// <summary>
/// Efecto visual de aturdimiento en los ojos del enemigo.
///
/// FLUJO:
/// 1. Cuando el enemigo es aturdido (StunFlash())
/// 2. Los ojos hacen un parpadeo rápido (4 flashes)
/// 3. Después se quedan encendidos hasta que termine el stun
/// 4. Al recuperarse, se apagan
///
/// Setup:
/// - Añade Lights (Point Light) en los ojos del enemigo (GameObjects hijos)
/// - Asígnalos al array 'eyeLights'
/// - El script se suscribe automáticamente al BehaviourTree para detectar el stun
/// </summary>
public class EnemyEyeFlash : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Lights de los ojos del enemigo (típicamente 2: uno por ojo)")]
    public Light[] eyeLights;

    [Tooltip("Referencia al BehaviourTree (auto-detecta si está en el mismo GameObject)")]
    public EnemyBehaviourTree behaviourTree;

    [Header("Colores")]
    [Tooltip("Color de los ojos al aturdir")]
    public Color stunColor = Color.white;
    [Tooltip("Intensidad de la luz al aturdir")]
    public float stunIntensity = 3f;

    [Header("Parpadeo")]
    [Tooltip("Número de parpadeos al ser aturdido")]
    public int blinkCount = 4;
    [Tooltip("Duración de cada flash")]
    public float blinkOnDuration = 0.08f;
    [Tooltip("Duración entre flashes")]
    public float blinkOffDuration = 0.06f;

    private Coroutine _currentRoutine;
    private bool _wasStunned = false;

    private void Awake()
    {
        if (behaviourTree == null)
            behaviourTree = GetComponent<EnemyBehaviourTree>();

        // Configurar los lights al inicio (apagados)
        foreach (var light in eyeLights)
        {
            if (light != null)
            {
                light.enabled = false;
                light.color = stunColor;
                light.intensity = stunIntensity;
            }
        }
    }

    private void Update()
    {
        if (behaviourTree == null) return;

        bool isStunned = behaviourTree.IsCurrentlyStunned;

        // Detectar el momento exacto del stun
        if (isStunned && !_wasStunned)
            StartFlashEffect();

        // Detectar el momento exacto del fin del stun
        if (!isStunned && _wasStunned)
            EndFlashEffect();

        _wasStunned = isStunned;
    }

    private void StartFlashEffect()
    {
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        _currentRoutine = StartCoroutine(FlashRoutine());
    }

    private void EndFlashEffect()
    {
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        SetLightsActive(false);
    }

    private IEnumerator FlashRoutine()
    {
        // Parpadeo rápido
        for (int i = 0; i < blinkCount; i++)
        {
            SetLightsActive(true);
            yield return new WaitForSeconds(blinkOnDuration);
            SetLightsActive(false);
            yield return new WaitForSeconds(blinkOffDuration);
        }

        // Dejar las luces encendidas durante el resto del stun
        SetLightsActive(true);
    }

    private void SetLightsActive(bool active)
    {
        foreach (var light in eyeLights)
        {
            if (light != null) light.enabled = active;
        }
    }
}
