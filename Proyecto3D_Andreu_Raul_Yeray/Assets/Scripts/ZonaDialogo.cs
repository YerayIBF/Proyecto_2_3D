using UnityEngine;
using StarterAssets;

[RequireComponent(typeof(Collider))]
public class ZonaDialogo : MonoBehaviour
{
    [TextArea(2, 4)]
    [SerializeField] public string texto = "";
    [SerializeField] public float duracion = 2f;
    [SerializeField] public bool soloUnaVez = false;

    [Tooltip("Si está activo, se reproduce al entrar sin pulsar nada. Si está desactivado, hay que pulsar interactuar.")]
    [SerializeField] public bool automatico = false;

    private bool jugadorDentro = false;
    private bool yaUsado = false;
    private StarterAssetsInputs _input;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (automatico) return;
        if (!jugadorDentro) return;
        if (soloUnaVez && yaUsado) return;
        if (_input == null) return;

        if (_input.interact)
        {
            _input.interact = false;
            Reproducir();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        jugadorDentro = true;
        _input = other.GetComponent<StarterAssetsInputs>();

        if (automatico)
        {
            if (soloUnaVez && yaUsado) return;
            Reproducir();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDentro = false;
            _input = null;
        }
    }

    private void Reproducir()
    {
        if (GameManager.instance != null)
            GameManager.instance.ReproducirVoz(texto, duracion);

        yaUsado = true;
    }
}