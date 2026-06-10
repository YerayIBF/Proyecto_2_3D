using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Playables;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool tieneLinterna = false;
    public bool tieneMegafono = false;
    public bool tieneLlave = false;
    public bool tieneLlaveFinal = false;
    public GameObject canvasMegafono;
    public TextMeshProUGUI textoEnergia;
    private GameObject megafono;
    public GameObject[] bateriaRayas;
    public Image bateriaFondo;
    public Image megafonoImg;
    public Sprite iconoBateria;
    public Sprite iconoSinBateria;
    public bool zonaActivacion = false;

    public bool zonaActivacion2 = false;
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
    public string mensajeLlaveRecogida = "He agafat una clau";

    [Header("Llaves recogidas (sistema con ID)")]
    private HashSet<string> _keysCollected = new HashSet<string>();

    [Header("HUD — Contador de pilas del megáfono")]
    [Tooltip("Texto que muestra el número de pilas del megáfono (ej: 'x2')")]
    public TextMeshProUGUI textoPilasMegafono;
    [Tooltip("Formato del texto. {0} se sustituye por el número")]
    public string formatoPilasMegafono = "x{0}";

    public Transform playerCameraRoot;
    public CinemachineCamera camaraVirtualSpline;
    public float duracionCinematica = 5f;

    [Header("Paneles")]
    public int totalPaneles = 5;
    private int _panelesCompletados = 0;
    public Animator puertaAnim;
    public CinemachineCamera camaraEvento;
    public float duracionCamara = 3f;
    public GameObject dialogo;

    [Header("Audio")]
    public AudioSource openSoundGarage;

    public MinijuegoElectric minijuegoActivo;

    private CinemachineSplineDolly splineDolly;
    private Coroutine cinematicaCoroutine;

    private int _lastPilaCount = -1;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
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

        if (iconoLlaveHUD != null)
            iconoLlaveHUD.SetActive(false);

        // Inicializar contador de pilas
        ActualizarContadorPilasMegafono();
    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        scriptJugador.enabled = true;
        StartCoroutine(QuitarTexto());
    }

    IEnumerator QuitarTexto()
    {
        yield return new WaitForSeconds(3f);
        textoTimelineInicial.gameObject.SetActive(false);
    }

    void Update()
    {
        // Actualizar el contador de pilas del megáfono si cambió
        ActualizarContadorPilasMegafono();
    }

    // ─── HUD pilas megáfono ──────────────────────────────────────────────────

    private void ActualizarContadorPilasMegafono()
    {
        if (textoPilasMegafono == null) return;
        if (PlayerStateMachine.Instance == null) return;

        int currentCount = PlayerStateMachine.Instance.MegafonoBatteryCount;

        if (currentCount != _lastPilaCount)
        {
            _lastPilaCount = currentCount;
            textoPilasMegafono.text = string.Format(formatoPilasMegafono, currentCount);
        }
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

    // ─── Llaves (sistema viejo, mantenido por compatibilidad) ────────────────

    public void RecogerLlave()
    {
        AddKey("principal");
    }

    public void RecogerLlaveFinal()
    {
        tieneLlaveFinal = true;
        ReproducirVoz("He trobat la clau de la sortida, he de trobar la sortida ràpid", 3f);
    }

    public void UsarLlave()
    {
        UseKey("principal");
    }

    // ─── Sistema de llaves con ID (nuevo) ────────────────────────────────────

    public void AddKey(string keyID)
    {
        if (string.IsNullOrEmpty(keyID)) return;

        _keysCollected.Add(keyID);
        tieneLlave = true;

        if (iconoLlaveHUD != null)
            iconoLlaveHUD.SetActive(true);

        if (!string.IsNullOrEmpty(mensajeLlaveRecogida))
            ReproducirVoz(mensajeLlaveRecogida, 3f);

        Debug.Log($"[GameManager] Llave '{keyID}' añadida.");
    }

    public bool HasKey(string keyID)
    {
        if (string.IsNullOrEmpty(keyID)) return false;
        return _keysCollected.Contains(keyID);
    }

    public void UseKey(string keyID)
    {
        if (string.IsNullOrEmpty(keyID)) return;

        _keysCollected.Remove(keyID);

        if (_keysCollected.Count == 0)
        {
            tieneLlave = false;
            if (iconoLlaveHUD != null)
                iconoLlaveHUD.SetActive(false);
        }

        Debug.Log($"[GameManager] Llave '{keyID}' usada.");
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

    public void ReproducirVoz(string subtitulo, float duracion)
    {
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
        camaraVirtualSpline.Priority = 100;

        scriptJugador.enabled = false;
        if (cinematicaCoroutine != null) StopCoroutine(cinematicaCoroutine);
        cinematicaCoroutine = StartCoroutine(RecorrerSpline());
    }

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

    public void PanelCompletado()
    {
        _panelesCompletados++;
        Debug.Log($"Paneles: {_panelesCompletados}/{totalPaneles}");

        if (_panelesCompletados >= totalPaneles)
        {
            if (openSoundGarage != null) openSoundGarage.Play();
            puertaAnim.SetTrigger("Abrir");
            StartCoroutine(MostrarCamaraEvento());

            if (dialogo != null)
                dialogo.SetActive(false);
        }
    }

    private IEnumerator MostrarCamaraEvento()
    {
        if (camaraEvento != null)
        {
            camaraEvento.Priority = 30;
            yield return new WaitForSeconds(duracionCamara);
            camaraEvento.Priority = 0;
        }
    }
}