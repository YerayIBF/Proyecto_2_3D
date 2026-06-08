using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class PanelEspecial : MonoBehaviour
{
    [Header("Enemigos a cegar (cannotSee = true)")]
    public enemigoaire[] enemigos;

    [Header("Luces a apagar")]
    public Light[] luces;

    [Header("Sonido")]
    public AudioSource audioSource;
    public AudioClip sonidoApagon;

    [Header("Cámara del evento")]
    public CinemachineCamera camaraEvento;
    public float duracionCamara = 3f;

    // Llamado desde TareaCables cuando se completa este panel
    public void CompletarPanelEspecial()
    {
        Debug.Log("[PanelEspecial] Panel completado, ejecutando apagón.");

        if (GameManager.instance != null)
                GameManager.instance.ReproducirVoz("Perfecte, ja he tallat la llum. Hauria d'anar amb la llanterna apagada i sense fer soroll.", 8f);

        // 1. Cegar a los enemigos
        foreach (enemigoaire e in enemigos)
            if (e != null) e.cannotSee = true;

        // 2. Apagar las luces
        foreach (Light l in luces)
            if (l != null) l.enabled = false;

        // 3. Reproducir sonido
        if (audioSource != null && sonidoApagon != null)
            audioSource.PlayOneShot(sonidoApagon);

        // 4. Enfocar la cámara del evento unos segundos
        if (camaraEvento != null)
            StartCoroutine(MostrarCamara());
    }

    private IEnumerator MostrarCamara()
    {
        camaraEvento.Priority = 30; // por encima de todo
        yield return new WaitForSeconds(duracionCamara);
        camaraEvento.Priority = 0;  // vuelve a la cámara normal
    }
}