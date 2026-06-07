using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using StarterAssets;

public class PanelElectricoInteraccion : MonoBehaviour
{
    [Header("Cinemachine")]
    public CinemachineCamera camaraJugador;
    public CinemachineCamera camaraPanel;

    [Header("Referencias jugador")]
    public ThirdPersonController controladorJugador;
    public GameObject modeloJugador;

    [Header("Minijuego")]
    public SelectorCablesMando selectorCables;

    [Header("UI")]
    public GameObject promptInteraccion;

    private StarterAssetsInputs _input;
    private bool _jugadorCerca    = false;
    private bool _minijuegoActivo = false;

    void Start()
    {
        if (promptInteraccion != null) promptInteraccion.SetActive(false);
        if (selectorCables    != null) selectorCables.enabled = false;
        if (camaraPanel       != null) camaraPanel.Priority   = 0;
    }

    void Update()
    {
        // Abrir con E / interact
        if (_jugadorCerca && !_minijuegoActivo && _input != null && _input.interact)
        {
            _input.interact = false;
            AbrirMinijuego();
            return;
        }

        // Salir con: Cancel (B/O), salto (botón Sur / Espacio), o Escape
        if (_minijuegoActivo && BotonSalirPulsado())
            CerrarMinijuego();
    }

    bool BotonSalirPulsado()
    {
        bool salir = false;

        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonEast.wasPressedThisFrame)  salir = true; // B / círculo
            if (Gamepad.current.buttonSouth.wasPressedThisFrame) salir = true; // A / X (salto)
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame) salir = true;
            if (Keyboard.current.spaceKey.wasPressedThisFrame)  salir = true; // salto
        }

        return salir;
    }

    void AbrirMinijuego()
    {
        _minijuegoActivo = true;

        if (controladorJugador != null) controladorJugador.bloqueado = true;
        if (modeloJugador      != null) modeloJugador.SetActive(false);
        if (camaraPanel        != null) camaraPanel.Priority   = 20;
        if (camaraJugador      != null) camaraJugador.Priority = 10;
        if (selectorCables     != null) selectorCables.enabled = true;
        if (promptInteraccion  != null) promptInteraccion.SetActive(false);

        Debug.Log("[Panel] Minijuego abierto.");
    }

    void CerrarMinijuego()
    {
        _minijuegoActivo = false;

        // Soltar cualquier cable agarrado ANTES de desactivar el selector
        if (selectorCables != null)
        {
            selectorCables.CancelarTodo();
            selectorCables.enabled = false;
        }

        if (controladorJugador != null) controladorJugador.bloqueado = false;
        if (modeloJugador      != null) modeloJugador.SetActive(true);
        if (camaraPanel        != null) camaraPanel.Priority   = 0;
        if (camaraJugador      != null) camaraJugador.Priority = 10;
        if (_jugadorCerca && promptInteraccion != null)
            promptInteraccion.SetActive(true);

        Debug.Log("[Panel] Minijuego cerrado.");
    }

    public void MinijuegoCompletado()
    {
        CerrarMinijuego();
        GameManager.instance.PanelCompletado();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _jugadorCerca = true;
        _input = other.GetComponent<StarterAssetsInputs>();
        if (promptInteraccion != null) promptInteraccion.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _jugadorCerca = false;
        _input = null;
        if (promptInteraccion != null) promptInteraccion.SetActive(false);
        if (_minijuegoActivo) CerrarMinijuego();
    }
}