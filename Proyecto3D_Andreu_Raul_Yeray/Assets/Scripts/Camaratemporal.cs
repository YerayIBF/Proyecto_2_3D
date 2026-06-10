using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using StarterAssets;

/// <summary>
/// Muestra esta cámara durante unos segundos y luego vuelve a la del jugador.
/// Ponlo en el GameObject de la cámara de la puerta (que NO se desactiva),
/// así la corrutina no se corta aunque la llave que lo dispara desaparezca.
/// </summary>
public class CamaraTemporal : MonoBehaviour
{
    [Header("Cámaras")]
    public CinemachineCamera camaraJugador;
    public CinemachineCamera camaraEsta;   // esta cámara (la de la puerta)
    public ThirdPersonController scriptJugador;

    public void Mostrar(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(MostrarCoroutine(duration));
    }

    private IEnumerator MostrarCoroutine(float duration)
    {
        if (camaraEsta != null)    camaraEsta.Priority    = 30;
        if (camaraJugador != null) camaraJugador.Priority = 10;

        if (scriptJugador != null) scriptJugador.enabled = false;

        yield return new WaitForSeconds(duration);

        if (camaraEsta != null)    camaraEsta.Priority    = 0;
        if (camaraJugador != null) camaraJugador.Priority = 10;

        if (scriptJugador != null) scriptJugador.enabled = true;
    }
}