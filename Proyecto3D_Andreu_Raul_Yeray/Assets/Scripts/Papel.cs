using UnityEngine;
using UnityEngine.InputSystem;

public class Papel : MonoBehaviour
{
    public string texto;
    public GameObject canvasLeer;
    public bool jugadorCerca = false;
    private bool estaLeyendo = false;

    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        if (jugadorCerca && BotonLeerPulsado())
        {
            if (!estaLeyendo)
                LeerPapel();
            else
                DejarDeLeerPapel();
        }

        // Orientar el canvas hacia la cámara (efecto billboard)
        if (canvasLeer != null && canvasLeer.activeSelf)
            OrientarCanvasACamara();
    }

    private void OrientarCanvasACamara()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        // El canvas mira hacia la cámara
        Vector3 direccion = canvasLeer.transform.position - _cam.transform.position;
        canvasLeer.transform.rotation = Quaternion.LookRotation(direccion);
    }

    private bool BotonLeerPulsado()
    {
        if (Input.GetKeyDown(KeyCode.E))
            return true;

        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            return true;

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            if (canvasLeer != null)
                canvasLeer.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            if (canvasLeer != null)
                canvasLeer.SetActive(false);
        }
    }

    void LeerPapel()
    {
        estaLeyendo = true;
        GameManager.instance.MostrarPapel(texto);

        if (canvasLeer != null)
            canvasLeer.SetActive(false);
    }

    void DejarDeLeerPapel()
    {
        estaLeyendo = false;
        GameManager.instance.CerrarPapel();

        if (canvasLeer != null && jugadorCerca)
            canvasLeer.SetActive(true);
    }
}