using UnityEngine;
using TMPro;

/// <summary>
/// Trigger que muestra el objetivo de los cuadros eléctricos con su contador.
/// Al entrar en la zona, activa el texto del HUD y lo mantiene actualizado
/// con el número de paneles completados del GameManager.
/// </summary>
public class ZonaObjetivo : MonoBehaviour
{
    [Header("HUD")]
    [Tooltip("GameObject del texto de objetivo en el HUD")]
    public GameObject objetivoHUD;
    [Tooltip("Componente de texto donde se muestra el contador")]
    public TextMeshProUGUI textoObjetivo;

    [Header("Textos")]
    [Tooltip("{0}=completados, {1}=total")]
    public string formatoObjetivo = "Repara els quadres elèctrics: {0}/{1}";
    public string objetivoCompletado = "Busca una sortida";

    [Header("Opciones")]
    public bool soloUnaVez = true;

    private bool _activado = false;

    void Start()
    {
        // El objetivo empieza oculto
        if (objetivoHUD != null)
            objetivoHUD.SetActive(false);
    }

    void Update()
    {
        // Mientras esté visible, mantener el contador actualizado
        if (_activado && textoObjetivo != null && GameManager.instance != null)
            ActualizarTexto();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (soloUnaVez && _activado) return;

        _activado = true;

        if (objetivoHUD != null)
            objetivoHUD.SetActive(true);

        ActualizarTexto();
    }

    private void ActualizarTexto()
    {
        if (textoObjetivo == null || GameManager.instance == null) return;

        int completados = GameManager.instance._panelesCompletados;
        int total       = GameManager.instance.totalPaneles;

        if (completados >= total)
            textoObjetivo.text = objetivoCompletado;
        else
            textoObjetivo.text = string.Format(formatoObjetivo, completados, total);
    }
}