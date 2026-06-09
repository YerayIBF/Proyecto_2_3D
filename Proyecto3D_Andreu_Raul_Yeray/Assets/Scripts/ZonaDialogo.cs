using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class ZonaDialogo : MonoBehaviour
{
    [TextArea(2, 4)]
    [SerializeField] public string texto = "";
    [SerializeField] public float duracion = 2f;
    [SerializeField] public bool soloUnaVez = false;

    [Tooltip("Si está activo, se reproduce al entrar sin pulsar nada. Si está desactivado, hay que pulsar la X.")]
    [SerializeField] public bool automatico = false;

    private bool jugadorDentro = false;
    private bool yaUsado = false;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        // Modo automático no necesita Update (se dispara en OnTriggerEnter)
        if (automatico) return;

        if (!jugadorDentro) return;
        if (soloUnaVez && yaUsado) return;

        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            Reproducir();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        jugadorDentro = true;

        // En modo automático se reproduce nada más entrar
        if (automatico)
        {
            if (soloUnaVez && yaUsado) return;
            Reproducir();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            jugadorDentro = false;
    }

    private void Reproducir()
    {
        if (GameManager.instance != null)
            GameManager.instance.ReproducirVoz(texto, duracion);

        yaUsado = true;
    }
}