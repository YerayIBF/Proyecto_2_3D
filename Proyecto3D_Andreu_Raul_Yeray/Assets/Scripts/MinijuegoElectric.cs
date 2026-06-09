using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class MinijuegoElectric : MonoBehaviour
{
    public string puzzle1;
    public GameObject panelMinijuego;
    public RectTransform aguja;       
    public RectTransform fondoBarra;
    public RectTransform imagenZonaVerde;

    public float velocidadAguja = 2f;
    [Range(0, 1)] public float zonaSeguraInicio = 0.7f; 
    [Range(0, 1)] public float zonaSeguraFin = 0.8f;

    private float progreso = 0f;
    private bool juegoActivo = false;
    public Animator puertaAnim;
    public Transform puertaIzquierda;
    public Transform puertaDerecha;
    public float velocidadApertura = 2f;
    public InputActionReference MinigameAction;
    public bool conAnimator = false;

    [Header("Audio")]
    public AudioSource openSound;
    public AudioSource openSoundGarage;

    [Header("Cámara del garage")]
    public CinemachineCamera camaraGarage;
    public float duracionCamaraGarage = 3f;

    // Booleanas de finalizado
    public bool _puertasCompletado = false;
    public bool _garageCompletado  = false;


    void OnEnable()
    {
        if (MinigameAction != null)
        {
            MinigameAction.action.Enable();
            MinigameAction.action.performed += OnMinigameGreen;
        }
    }

    void OnDisable()
    {
        if (MinigameAction != null)
        {
            MinigameAction.action.performed -= OnMinigameGreen;
            MinigameAction.action.Disable();
        }
    }

    private void OnMinigameGreen(InputAction.CallbackContext context)
    {
        if (juegoActivo)
        {
            ComprobarExito();
        }
    }
    void Start()
    {
        //Si el puzzle ya ha sido completado antes y se ha guardado el progreso se abre la puerta al iniciar
        if (ProgressManager.instance.PuzzleCompletado(puzzle1))
        {
            //puertaAnim.SetTrigger("Abrir");
            if (puertaIzquierda != null)
            {
                puertaIzquierda.rotation *= Quaternion.Euler(0, -90f, 0);
            }
            if (puertaDerecha != null)
            {
                puertaDerecha.rotation *= Quaternion.Euler(0, 90f, 0);
            }
            _puertasCompletado = true;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update() {
        if (!juegoActivo) return;

        //Mover flecha por todo el ancho del fondo de la barra
        progreso += Time.deltaTime * velocidadAguja;
        if (progreso > 1) progreso = 0;

        float anchoTotal = fondoBarra.rect.width;
        aguja.anchoredPosition = new Vector2(progreso * anchoTotal - (anchoTotal / 2), 0);

        if (Input.GetKeyDown(KeyCode.X)) { 
            ComprobarExito();
        }
    }

    void ComprobarExito() {
        float margenExito = 0.05f;

        if (progreso >= zonaSeguraInicio - margenExito && progreso <= zonaSeguraFin + margenExito) {
            Debug.Log("Correcto");
            //guardamos el progreso llamando al script de progress manager
            if (conAnimator)
            {
                if (_garageCompletado) { TerminarJuego(); return; }
                _garageCompletado = true;

                if (openSoundGarage != null) openSoundGarage.Play();
                puertaAnim.SetTrigger("Abrir");

                if (camaraGarage != null) StartCoroutine(MostrarCamaraGarage());
            }
            else
            {
                if (_puertasCompletado) { TerminarJuego(); return; }
                _puertasCompletado = true;

                if (openSound != null) openSound.Play();
                StartCoroutine(AbrirPuertas());
                GameManager.instance.ReproducirVoz("Sembla que s'ha obert una porta, hauria d'anar a mirar", 4f);
                ProgressManager.instance.RegistrarPuzzleCompletado(puzzle1);
                
            }
            TerminarJuego();
            //Activar electricidad
        } else {
            Debug.Log("Incorrecto - Ruido fuerte");
            //Emitir ruido en esa zona
            TerminarJuego();
        }
    }

   public void IniciarJuego() {
    Debug.Log("=== IniciarJuego LLAMADO ===");
    if (_puertasCompletado) { Debug.Log("Bloqueado: puertas completado"); return; }
    conAnimator = false;
    
    float anchoZona = zonaSeguraFin - zonaSeguraInicio; 
    zonaSeguraInicio = Random.Range(0.1f, 0.85f);      
    zonaSeguraFin = zonaSeguraInicio + anchoZona;        

    if (zonaSeguraFin > 1f)
    {
        zonaSeguraFin = 1f;
        zonaSeguraInicio = zonaSeguraFin - anchoZona;
    }

    juegoActivo = true;
    panelMinijuego.SetActive(true);
    Debug.Log("Panel activado. activeInHierarchy = " + panelMinijuego.activeInHierarchy);
    progreso = 0;
    ZonaVerdeSetup();
    }


    public void IniciarJuegoAnimator() {
        if (_garageCompletado) return;
        conAnimator = true;
        //Cada vez que se abre el minijuego se cambia la posicion de la zona verde para que sea mas aleatorio
        float anchoZona = zonaSeguraFin - zonaSeguraInicio; 
        zonaSeguraInicio = Random.Range(0.1f, 0.85f);      
        zonaSeguraFin = zonaSeguraInicio + anchoZona;        

        if (zonaSeguraFin > 1f)
        {
            zonaSeguraFin = 1f;
            zonaSeguraInicio = zonaSeguraFin - anchoZona;
        }

        juegoActivo = true;
        panelMinijuego.SetActive(true);
        progreso = 0;
        ZonaVerdeSetup();
    }

    void TerminarJuego() {
        juegoActivo = false;
        panelMinijuego.SetActive(false);
    }

    //Preparar zona verde para calcular correctamente la posicion de la zona segura al pulsar E
    void ZonaVerdeSetup()
    {
        if (imagenZonaVerde == null || fondoBarra == null) return;

        float anchoTotal = fondoBarra.rect.width;

        float anchoZona = (zonaSeguraFin - zonaSeguraInicio) * anchoTotal;
        
        //imagenZonaVerde.sizeDelta = new Vector2(anchoZona, imagenZonaVerde.sizeDelta.y);

        float centroProgreso = (zonaSeguraInicio + zonaSeguraFin) / 2f;
        float posX = (centroProgreso * anchoTotal) - (anchoTotal / 2f);

        imagenZonaVerde.anchoredPosition = new Vector2(posX, 0);
    }

    IEnumerator AbrirPuertas()
    {
       //Rotar las puertas para que se abran y el jugador pueda pasar
        Quaternion rotObjetivoIzq = puertaIzquierda.rotation * Quaternion.Euler(0, -90f, 0);
        Quaternion rotObjetivoDer = puertaDerecha.rotation * Quaternion.Euler(0, 90f, 0);

        while (Quaternion.Angle(puertaIzquierda.rotation, rotObjetivoIzq) > 0.1f)
        {
            puertaIzquierda.rotation = Quaternion.Lerp(
                puertaIzquierda.rotation, rotObjetivoIzq, Time.deltaTime * velocidadApertura);
            puertaDerecha.rotation = Quaternion.Lerp(
                puertaDerecha.rotation, rotObjetivoDer, Time.deltaTime * velocidadApertura);
            yield return null;
        }

        puertaIzquierda.rotation = rotObjetivoIzq;
        puertaDerecha.rotation = rotObjetivoDer;
    }

    IEnumerator MostrarCamaraGarage()
    {
        camaraGarage.Priority = 30;
        yield return new WaitForSeconds(duracionCamaraGarage);
        camaraGarage.Priority = 0;
    }
}