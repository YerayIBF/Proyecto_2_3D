using UnityEngine;

public class ActivarElectricidad : MonoBehaviour
{
    public BoxCollider electricidadCollider;
    public GameObject canvasElectricidad;
    public Animator puertaAnim;

    [Header("Minijuego de esta zona")]
    public MinijuegoElectric minijuego; // arrastra el MinijuegoElectric de ESTA puerta

    void Start()
    {
        canvasElectricidad.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            canvasElectricidad.SetActive(true);
            GameManager.instance.zonaActivacion = true;
            GameManager.instance.minijuegoActivo = minijuego; // guarda cuál es el minijuego de esta zona
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            canvasElectricidad.SetActive(false);
            GameManager.instance.zonaActivacion = false;
        }
    }
}