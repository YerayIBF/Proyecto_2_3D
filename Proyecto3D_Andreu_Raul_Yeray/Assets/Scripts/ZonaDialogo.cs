using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class ZonaDialogo : MonoBehaviour
{
    [TextArea(2, 4)]
    [SerializeField] public string texto = "";
    [SerializeField] public float duracion = 2f;
    [SerializeField] public bool soloUnaVez = false;

    private bool jugadorDentro = false;
    private bool yaUsado = false;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (!jugadorDentro) return;
        if (soloUnaVez && yaUsado) return;

        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            if (GameManager.instance != null)
                GameManager.instance.ReproducirVoz("", texto, duracion);

            yaUsado = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            jugadorDentro = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            jugadorDentro = false;
    }
}
