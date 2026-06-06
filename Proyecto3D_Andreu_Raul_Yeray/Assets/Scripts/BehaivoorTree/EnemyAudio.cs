using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Gestor de audios del enemigo. AudioSources propios con audio 3D espacializado.
///
/// Tipos de audio:
/// - Pasos: loop automático según velocidad (caminar/correr)
/// - Gruñidos ambientales: aleatorios mientras patrulla
/// - Alerta: al detectar al jugador
/// - Ataque: al golpear
/// </summary>
public class EnemyAudio : MonoBehaviour
{
    [Header("AudioClips")]
    public AudioClip walkSteps;
    public AudioClip runSteps;
    public AudioClip[] gruñidos;
    public AudioClip[] alertas;
    public AudioClip ataque;

    [Header("Volúmenes (0-1)")]
    public float stepsVolume = 0.6f;
    public float voiceVolume = 1f;

    [Header("Intervalos")]
    [Tooltip("Cada cuántos segundos puede sonar un gruñido ambiental")]
    public float gruñidoIntervalMin = 5f;
    public float gruñidoIntervalMax = 12f;

    [Header("Audio 3D")]
    public float maxAudioDistance = 30f;
    public float minAudioDistance = 2f;

    // ─── Internal ────────────────────────────────────────────────────────────

    private AudioSource _stepsSource;
    private AudioSource _voiceSource;
    private NavMeshAgent _agent;

    private float _nextGruñidoTime;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null) _agent = GetComponentInParent<NavMeshAgent>();

        _stepsSource = CreateAudioSource("StepsAudio", true, stepsVolume);
        _voiceSource = CreateAudioSource("VoiceAudio", false, voiceVolume);

        _nextGruñidoTime = Time.time + Random.Range(gruñidoIntervalMin, gruñidoIntervalMax);
    }

    private AudioSource CreateAudioSource(string name, bool loop, float volume)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        AudioSource src = go.AddComponent<AudioSource>();
        src.loop = loop;
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = minAudioDistance;
        src.maxDistance = maxAudioDistance;
        src.volume = volume;

        return src;
    }

    private void Update()
    {
        if (_agent == null) return;

        UpdateFootsteps();
        UpdateAmbientGruñido();
    }

    private void UpdateFootsteps()
    {
        float speed = _agent.velocity.magnitude;

        if (speed < 0.2f)
        {
            if (_stepsSource.isPlaying) _stepsSource.Stop();
            return;
        }

        bool running = speed > 3f;
        AudioClip targetClip = running ? runSteps : walkSteps;
        if (targetClip == null) return;

        if (_stepsSource.clip != targetClip)
        {
            _stepsSource.clip = targetClip;
            _stepsSource.Play();
        }
        else if (!_stepsSource.isPlaying)
        {
            _stepsSource.Play();
        }
    }

    private void UpdateAmbientGruñido()
    {
        if (Time.time < _nextGruñidoTime) return;
        if (gruñidos == null || gruñidos.Length == 0) return;
        if (_voiceSource.isPlaying) return;

        PlayRandom(gruñidos);
        _nextGruñidoTime = Time.time + Random.Range(gruñidoIntervalMin, gruñidoIntervalMax);
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    public void PlayAttack()
    {
        if (ataque != null)
            _voiceSource.PlayOneShot(ataque, voiceVolume);
    }

    public void PlayAlert()
    {
        if (alertas == null || alertas.Length == 0) return;
        PlayRandom(alertas);
    }

    /// <summary>Para todos los sonidos (pasos y voz). Útil al stunearse.</summary>
    public void StopAll()
    {
        if (_stepsSource.isPlaying) _stepsSource.Stop();
        if (_voiceSource.isPlaying) _voiceSource.Stop();
    }

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
            _voiceSource.PlayOneShot(clip, voiceVolume);
    }
}
