using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Playables;
using StarterAssets;
using TMPro;
using System.Collections;
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool tieneLinterna = false;
    public bool tieneMegafono = false;
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

    //public GameObject canvasPapel;
    //public TextMeshProUGUI textoPapel;
    private bool leyendoPapel = false;

    public TextMeshProUGUI textoSubtitulo;
    public GameObject canvasSubtitulo;

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
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scriptJugador.enabled = false;
        timelineInicial.stopped += OnTimelineFinished;
        timelineInicial.Play();
        megafono = GameObject.FindGameObjectWithTag("Megafono");
        megafonoImg.sprite = iconoBateria;
    }

    //Al finalizar la cinematica el personaje puede moverse y el texto desaparece
    private void OnTimelineFinished(PlayableDirector director)
    {
        scriptJugador.enabled = true;
        textoTimelineInicial.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

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

    //Actualizar energia del megafono de forma visual
    public void ActualizarEnergia(float energiaActual, float maxEnergia)
    {
        //textoEnergia.text = Mathf.RoundToInt(energiaActual) + " / " + Mathf.RoundToInt(maxEnergia);
        float porcentaje = energiaActual/maxEnergia;
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

    //Reproducir cinematica inicial
    public void ReproducirTimelineInicial()
    {
        timelineInicial.Play();
    }

    /*public void MostrarPapel(string texto)
    {
        textoPapel.text = texto;
        canvasPapel.SetActive(true);
        leyendoPapel = true;
        Time.timeScale = 0f; 
    }

    public void CerrarPapel()
    {
        canvasPapel.SetActive(false);
        leyendoPapel = false;
        Time.timeScale = 1f; 
    }*/

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
}
