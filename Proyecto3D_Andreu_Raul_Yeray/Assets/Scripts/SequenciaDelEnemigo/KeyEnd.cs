using UnityEngine;
using Unity.Cinemachine; 
using System.Collections;
using StarterAssets;

public class KeyEnd : MonoBehaviour
{
    public CinemachineCamera camaraJugador;
    public CinemachineCamera camaraPuerta;
    public ThirdPersonController scriptJugador;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MostrarPuerta(float duration)
    {
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
