using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Playables;
using StarterAssets;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using Unity.Cinemachine; 
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool tieneLinterna = false;
    public bool tieneMegafono = false;
    public bool tieneLlave = false;   // ← NUEVO
    public GameObject canvasMegafono;
    public TextMeshProUGUI textoEnergia;
    private GameObject megafono;
    public GameObject[] bateriaRayas;
    public Image bateriaFondo;
    public Image megafonoImg;
    public Sprite iconoBateria;
    public Sprite iconoSinBateria;
    public bool zonaActivacion = false;
    public ThirdPersonController scriptJugador;

    public PlayableDirector timelineInicial;
    public TextMeshProUGUI textoTimelineInicial;
    public PlayableDirector timelineEnemigo;

    public GameObject canvasPapel;
    public TextMeshProUGUI textoPapel;
    private bool leyendoPapel = false;

    public TextMeshProUGUI textoSubtitulo;
    public GameObject canvasSubtitulo;

    [Header("Llave (opcional, feedback visual)")]
    [Tooltip("Icono de la llave en el HUD que se activa cuando la recoges")]
    public GameObject iconoLlaveHUD;
    [Tooltip("Texto que aparece al recoger la llave (opcional)")]
    public string mensajeLlaveRecogida = "Has recogido la llave";
    public Transform playerCameraRoot;
    public CinemachineCamera camaraVirtualSpline; 
    public float duracionCinematica = 5f; 

    private CinemachineSplineDolly splineDolly;
    private Coroutine cinematicaCoroutine;
    //public GameObject camaraVirtualJugador; 

    //public InputActionReference interactAction;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            //InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;

        }
        else
        {
            Destroy(gameObject);
        }

        if (camaraVirtualSpline != null)
        {
            splineDolly = camaraVirtualSpline.GetComponent<CinemachineSplineDolly>();
            camaraVirtualSpline.Priority = 0; 
        }
    }

    void Start()
    {
        scriptJugador.enabled = false;
        timelineInicial.stopped += OnTimelineFinished;
        timelineInicial.Play();
        megafono = GameObject.FindGameObjectWithTag("Megafono");
        megafonoImg.sprite = iconoBateria;

        // Asegurarse de que el icono de la llave está oculto al inicio
        if (iconoLlaveHUD != null)
            iconoLlaveHUD.SetActive(false);
    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        scriptJugador.enabled = true;
        textoTimelineInicial.gameObject.SetActive(false);
    }

    void Update()
    {

    }

    // ─── Megáfono ────────────────────────────────────────────────────────────

    public void RecogerMegafono()
    {
        tieneMegafono = true;
    }

    public void MostrarCanvasMegafono(bool mostrar)
    {
        canvasMegafono.SetActive(mostrar);

        Megafono megafonoScript = megafono.GetComponent<Megafono>();

        if (megafonoScript != null)
        {
            ActualizarEnergia(megafonoScript.energiaActual, megafonoScript.maxEnergia);
        }
    }

    public void ActualizarEnergia(float energiaActual, float maxEnergia)
    {
        float porcentaje = energiaActual / maxEnergia;
        float limite = porcentaje * bateriaRayas.Length;
        int rayas = Mathf.CeilToInt(porcentaje * bateriaRayas.Length);

        Color colorFondo;

        if (porcentaje <= 0.25f)
        {
            ColorUtility.TryParseHtmlString("#F7000A", out colorFondo);
            megafonoImg.sprite = iconoSinBateria;
        }
        else
        {
            ColorUtility.TryParseHtmlString("#4EF700", out colorFondo);
            megafonoImg.sprite = iconoBateria;
        }

        bateriaFondo.color = colorFondo;

        for (int i = 0; i < bateriaRayas.Length; i++)
        {
            if (i < rayas)
            {
                bateriaRayas[i].SetActive(true);

                if (rayas <= 2)
                    bateriaRayas[i].GetComponent<Image>().color = Color.red;
                else
                    bateriaRayas[i].GetComponent<Image>().color = Color.black;
            }
            else
            {
                bateriaRayas[i].SetActive(false);
            }
        }
    }

    // ─── Llave ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Llamado por KeyPickup cuando el jugador recoge la llave.
    /// </summary>
    public void RecogerLlave()
    {
        tieneLlave = true;

        // Mostrar icono en el HUD
        if (iconoLlaveHUD != null)
            iconoLlaveHUD.SetActive(true);

        // Mostrar subtítulo o mensaje
        if (!string.IsNullOrEmpty(mensajeLlaveRecogida))
            ReproducirVoz("", mensajeLlaveRecogida, 3f);

        Debug.Log("[GameManager] Llave recogida.");
    }

    /// <summary>
    /// Llamado por KeyDoor (o cualquier puerta) para gastar la llave al usarla.
    /// Si quieres que la llave sea reutilizable, NO llames a este método.
    /// </summary>
    public void UsarLlave()
    {
        tieneLlave = false;

        if (iconoLlaveHUD != null)
            iconoLlaveHUD.SetActive(false);

        Debug.Log("[GameManager] Llave usada.");
    }

    // ─── Otros ───────────────────────────────────────────────────────────────

    public void ReproducirTimelineInicial()
    {
        timelineInicial.Play();
    }

    public void MostrarPapel(string texto)
    {
        textoPapel.text = texto;
        canvasPapel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void CerrarPapel()
    {
        canvasPapel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void MostrarSubtitulo(string texto)
    {
        textoSubtitulo.text = texto;
        canvasSubtitulo.SetActive(true);
    }

    public void OcultarSubtitulo()
    {
        canvasSubtitulo.SetActive(false);
    }

    public void ReproducirVoz(string nombreAudio, string subtitulo, float duracion)
    {
        if (nombreAudio != "")
        {
            AudioManager.Instance.PlaySFX(nombreAudio);
        }

        if (subtitulo != "")
        {
            StartCoroutine(MostrarSubtituloCoroutine(subtitulo, duracion));
        }
    }

    IEnumerator MostrarSubtituloCoroutine(string texto, float duracion)
    {
        MostrarSubtitulo(texto);
        yield return new WaitForSeconds(duracion);
        OcultarSubtitulo();
    }

    public void ActivarTimelineLlave()
    {
        //camaraVirtualJugador.SetActive(false);
        camaraVirtualSpline.Priority = 100;

        scriptJugador.enabled = false;
       if (cinematicaCoroutine != null) StopCoroutine(cinematicaCoroutine);
        cinematicaCoroutine = StartCoroutine(RecorrerSpline());
    }

    /*private void OnTimelineFinalizado(PlayableDirector director)
    {
        scriptJugador.enabled = true;
    }*/

    private IEnumerator RecorrerSpline()
    {
        float tiempoPasado = 0f;

        while (tiempoPasado < duracionCinematica)
        {
            tiempoPasado += Time.deltaTime;
            
            float progreso = tiempoPasado / duracionCinematica;
            
            splineDolly.CameraPosition = progreso;

            yield return null; 
        }

        splineDolly.CameraPosition = 1f;

        FinalizarCinematica();
    }

    private void FinalizarCinematica()
    {
        camaraVirtualSpline.Priority = 0;

        splineDolly.CameraPosition = 0f;

        scriptJugador.enabled = true;
    }

    /*public void PanelCompletado()
    {
        _panelesCompletados++;
        Debug.Log($"Paneles: {_panelesCompletados}/{totalPaneles}");

        if (_panelesCompletados >= totalPaneles)
            puertaAnim.SetTrigger("Abrir");
    }*/

}