using UnityEngine;

public class ActivarElectricidad2 : MonoBehaviour
{
    public BoxCollider electricidadCollider;
    public GameObject canvasElectricidad;

    [Header("Minijuego de esta zona")]
    public MinijuegoElectric minijuego; // arrastra el MinijuegoElectric del garage

    void Start()
    {
        canvasElectricidad.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            canvasElectricidad.SetActive(true);
            GameManager.instance.zonaActivacion2 = true;
            GameManager.instance.minijuegoActivo = minijuego;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            canvasElectricidad.SetActive(false);
            GameManager.instance.zonaActivacion2 = false;
        }
    }
}