using UnityEngine;

public class MinijuegoElectric : MonoBehaviour
{
    public GameObject panelMinijuego;
    public RectTransform aguja;       
    public RectTransform fondoBarra;
    public RectTransform imagenZonaVerde;

    public float velocidadAguja = 2f;
    [Range(0, 1)] public float zonaSeguraInicio = 0.7f; 
    [Range(0, 1)] public float zonaSeguraFin = 0.8f;

    private float progreso = 0f;
    private bool juegoActivo = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update() {
        if (!juegoActivo) return;

        //Mover flecha por todo el ancho del fondo de la barra
        progreso += Time.deltaTime * velocidadAguja;
        if (progreso > 1) progreso = 0;

        float anchoTotal = fondoBarra.rect.width;
        aguja.anchoredPosition = new Vector2(progreso * anchoTotal - (anchoTotal / 2), 0);

        if (Input.GetKeyDown(KeyCode.E)) { 
            ComprobarExito();
        }
    }

    void ComprobarExito() {
        if (progreso >= zonaSeguraInicio && progreso <= zonaSeguraFin) {
            Debug.Log("Correcto");
            TerminarJuego();
            //Activar electricidad
        } else {
            Debug.Log("Incorrecto - Ruido fuerte");
            //Emitir ruido en esa zona
            TerminarJuego();
        }
    }

    public void IniciarJuego() {
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
}
