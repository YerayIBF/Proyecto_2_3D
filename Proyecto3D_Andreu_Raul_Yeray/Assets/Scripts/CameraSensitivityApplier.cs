using UnityEngine;
using StarterAssets;

/// <summary>
/// Aplica la sensibilidad de cámara guardada en PlayerPrefs.
/// Multiplica el valor de inputs.look ANTES de que ThirdPersonController lo lea.
///
/// El valor se lee de PlayerPrefs (clave "CameraSensitivity") que escribe
/// el MenuPausa cuando el usuario mueve el slider de sensibilidad.
///
/// Setup:
/// 1. Añade este componente al GameObject del Player (donde está ThirdPersonController)
/// 2. No requiere configuración en el Inspector.
/// </summary>
[DefaultExecutionOrder(-100)]
public class CameraSensitivityApplier : MonoBehaviour
{
    private const string PREF_KEY = "CameraSensitivity";

    private StarterAssetsInputs _inputs;
    private float _sensitivity = 1f;

    private void Awake()
    {
        _inputs = GetComponent<StarterAssetsInputs>();
        if (_inputs == null)
            _inputs = GetComponentInChildren<StarterAssetsInputs>();

        _sensitivity = PlayerPrefs.GetFloat(PREF_KEY, 1f);
    }

    private void Update()
    {
        // Recargar sensibilidad cada frame por si se cambió en el menú de pausa
        _sensitivity = PlayerPrefs.GetFloat(PREF_KEY, 1f);

        if (_inputs == null) return;

        // Multiplicar el look por la sensibilidad
        _inputs.look *= _sensitivity;
    }
}