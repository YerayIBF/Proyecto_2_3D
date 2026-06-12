using UnityEngine;

public class LightEffect : MonoBehaviour
{
    private Light _light;

    [Header("Tiempo encendida (estable)")]
    [Tooltip("Tiempo mínimo que la luz se mantiene fija antes de parpadear")]
    public float minTiempoEstable = 3f;
    [Tooltip("Tiempo máximo que la luz se mantiene fija antes de parpadear")]
    public float maxTiempoEstable = 15f;

    [Header("Parpadeo")]
    [Tooltip("Cuánto dura el parpadeo cada vez")]
    public float duracionParpadeo = 1f;
    public float minIntensity = 0.1f;
    public float maxIntensity = 2f;
    [Tooltip("Velocidad del cambio de intensidad durante el parpadeo")]
    public float velocidadParpadeo = 25f;

    private float _timer;
    private bool  _parpadeando = false;
    private float _targetIntensity;

    void Start()
    {
        _light = GetComponent<Light>();
        _light.intensity = maxIntensity;
        ProgramarSiguienteParpadeo();
    }

    void Update()
    {
        _timer -= Time.deltaTime;

        if (_parpadeando)
        {
            // Durante el parpadeo: saltar entre intensidades aleatorias muy rápido
            _light.intensity = Mathf.Lerp(_light.intensity, _targetIntensity, Time.deltaTime * velocidadParpadeo);

            if (Mathf.Abs(_light.intensity - _targetIntensity) < 0.1f)
                _targetIntensity = Random.Range(minIntensity, maxIntensity);

            // Fin del parpadeo: volver a luz estable y programar el siguiente
            if (_timer <= 0f)
            {
                _parpadeando = false;
                _light.intensity = maxIntensity;
                ProgramarSiguienteParpadeo();
            }
        }
        else
        {
            // Estable: la luz se queda fija hasta que toque parpadear
            if (_timer <= 0f)
            {
                _parpadeando = true;
                _timer = duracionParpadeo;
                _targetIntensity = minIntensity;
            }
        }
    }

    // Cada "tick" elige un tiempo aleatorio distinto entre min y max
    void ProgramarSiguienteParpadeo()
    {
        _timer = Random.Range(minTiempoEstable, maxTiempoEstable);
    }
}