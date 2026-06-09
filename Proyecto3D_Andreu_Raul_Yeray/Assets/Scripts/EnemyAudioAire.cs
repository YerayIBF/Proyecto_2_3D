using UnityEngine;
using System.Collections;

/// <summary>
/// Sistema de audio de gruñidos para el enemigoaire.
/// Reproduce gruñidos aleatorios cada cierto tiempo con pitch y volumen ajustables.
/// Audio 3D espacializado (se oye más fuerte cerca del enemigo).
/// </summary>
public class EnemyAudioAire : MonoBehaviour
{
    [Header("Clips de gruñidos")]
    [Tooltip("Lista de gruñidos. El script elige uno aleatorio cada vez.")]
    public AudioClip[] grunidos;

    [Header("Frecuencia")]
    [Tooltip("Tiempo mínimo entre gruñidos (segundos)")]
    public float intervalMin = 5f;
    [Tooltip("Tiempo máximo entre gruñidos (segundos)")]
    public float intervalMax = 12f;

    [Header("Volumen")]
    [Range(0f, 1f)]
    [Tooltip("Volumen de los gruñidos")]
    public float volume = 1f;

    [Header("Pitch (tono)")]
    [Tooltip("Pitch mínimo (más grave). 1 = normal")]
    public float pitchMin = 0.85f;
    [Tooltip("Pitch máximo (más agudo). 1 = normal")]
    public float pitchMax = 1.15f;

    [Header("Audio 3D")]
    [Tooltip("Distancia máxima a la que se oye")]
    public float maxAudioDistance = 25f;
    [Tooltip("Distancia mínima (a partir de ahí empieza a bajar el volumen)")]
    public float minAudioDistance = 2f;

    private AudioSource _audioSource;

    private void Awake()
    {
        // Crear o coger el AudioSource
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        // Configurar como 3D
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 1f; // Totalmente 3D
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
        _audioSource.minDistance = minAudioDistance;
        _audioSource.maxDistance = maxAudioDistance;
        _audioSource.volume = volume;
    }

    private void OnEnable()
    {
        StartCoroutine(GrunidoLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    // ─── Loop de gruñidos ────────────────────────────────────────────────────

    private IEnumerator GrunidoLoop()
    {
        // Esperar un poco al inicio antes del primer gruñido
        yield return new WaitForSeconds(Random.Range(2f, intervalMax));

        while (true)
        {
            PlayRandomGrunido();
            float waitTime = Random.Range(intervalMin, intervalMax);
            yield return new WaitForSeconds(waitTime);
        }
    }

    /// <summary>
    /// Reproduce un gruñido aleatorio de la lista.
    /// </summary>
    public void PlayRandomGrunido()
    {
        if (grunidos == null || grunidos.Length == 0) return;
        if (_audioSource == null) return;

        AudioClip clip = grunidos[Random.Range(0, grunidos.Length)];
        if (clip == null) return;

        // Aplicar pitch aleatorio
        _audioSource.pitch = Random.Range(pitchMin, pitchMax);
        _audioSource.volume = volume;
        _audioSource.PlayOneShot(clip);
    }

    // ─── Actualizar valores en runtime ───────────────────────────────────────

    private void Update()
    {
        // Sincronizar volumen y distancias por si se cambian en runtime
        if (_audioSource != null)
        {
            _audioSource.volume = volume;
            _audioSource.minDistance = minAudioDistance;
            _audioSource.maxDistance = maxAudioDistance;
        }
    }
}
