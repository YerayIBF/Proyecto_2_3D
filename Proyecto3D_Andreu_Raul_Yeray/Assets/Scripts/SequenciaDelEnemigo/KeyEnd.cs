using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using StarterAssets;

/// <summary>
/// Llave final del nivel con identificador.
/// Al recogerse, registra la llave en el GameManager con su ID
/// y cambia a la cámara que enfoca la puerta de salida durante unos segundos.
/// </summary>
public class KeyEnd : MonoBehaviour
{
    [Header("Identificador")]
    [Tooltip("ID de esta llave final. Debe coincidir con el de la puerta que abre.")]
    public string keyID = "final";

    [Header("Cámaras")]
    public CinemachineCamera camaraJugador;
    public CinemachineCamera camaraPuerta;
    public ThirdPersonController scriptJugador;

    void Start()
    {

    }

    void Update()
    {

    }

    /// <summary>
    /// Llamado por CogerObjeto.AnimatorCogerObjeto() cuando se recoge la llave final.
    /// </summary>
    public void MostrarPuerta(float duration)
    {
        // Registrar la llave en el GameManager con su ID
        if (GameManager.instance != null)
        {
            GameManager.instance.AddKey(keyID);
        }

        StartCoroutine(CambiarCamara(duration));
    }

    IEnumerator CambiarCamara(float duration)
    {
        camaraPuerta.Priority = 20;
        camaraJugador.Priority = 10;

        scriptJugador.enabled = false;

        yield return new WaitForSeconds(duration);

        camaraPuerta.Priority = 10;
        camaraJugador.Priority = 20;

        scriptJugador.enabled = true;
    }
}