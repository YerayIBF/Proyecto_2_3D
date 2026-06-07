using UnityEngine;
using UnityEngine.InputSystem;

public class SelectorCablesMando : MonoBehaviour
{
    public static SelectorCablesMando Instance { get; private set; }

    [Header("Referencias")]
    public GameObject[] cablesObj;
    private Cable[] cables;

    [Header("Resaltado")]
    public Color colorSeleccion = Color.white;

    private int   _indiceActual       = 0;
    private Cable _cableArrastrando   = null;
    private float _cooldownNavegacion = 0f;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        // Cada vez que se activa el minijuego, reconstruye la lista y selecciona el primero
        ConstruirLista();
        _indiceActual = 0;
        _cableArrastrando = null;
        SeleccionarCableActual();
    }

    void ConstruirLista()
    {
        cables = new Cable[cablesObj.Length];
        for (int i = 0; i < cablesObj.Length; i++)
            cables[i] = cablesObj[i] != null ? cablesObj[i].GetComponentInChildren<Cable>() : null;
    }

    void Update()
    {
        if (_cooldownNavegacion > 0f) _cooldownNavegacion -= Time.deltaTime;

        if (_cableArrastrando == null)
            NavegarCables();
        else
            GestionarArrastre();
    }

    void NavegarCables()
    {
        if (cables == null || cables.Length == 0) return;

        float v = LeerVertical();

        if (_cooldownNavegacion <= 0f)
        {
            if (v < -0.5f)
            {
                AvanzarIndice(1);
                _cooldownNavegacion = 0.25f;
            }
            else if (v > 0.5f)
            {
                AvanzarIndice(-1);
                _cooldownNavegacion = 0.25f;
            }
        }

        if (BotonAccionPulsado())
        {
            Cable cable = cables[_indiceActual];
            if (cable != null && !cable.Conectado)
            {
                _cableArrastrando = cable;
                cable.IniciarArrastreMando();
                DesresaltarTodos();
            }
        }
    }

    void GestionarArrastre()
    {
        if (BotonAccionPulsado())
        {
            _cableArrastrando.SoltarMando();
            _cableArrastrando = null;
            SeleccionarCableActual();
        }
    }

    public void SiguienteCable()
    {
        _cableArrastrando = null;
        StartCoroutine(LimpiarYSeleccionar());
    }

    // Llamado al cerrar el panel — suelta cualquier cable agarrado
    public void CancelarTodo()
    {
        if (_cableArrastrando != null)
        {
            _cableArrastrando.SoltarMando();
            _cableArrastrando = null;
        }
    }

    private System.Collections.IEnumerator LimpiarYSeleccionar()
    {
        yield return null;

        for (int i = 0; i < cables.Length; i++)
            if (cables[i] != null && cables[i].Conectado)
                cables[i] = null;

        SeleccionarCableActual();
    }

    void AvanzarIndice(int direccion)
    {
        int inicio = _indiceActual;
        do
        {
            _indiceActual = (_indiceActual + direccion + cables.Length) % cables.Length;
        }
        while (cables[_indiceActual] == null && _indiceActual != inicio);

        SeleccionarCableActual();
    }

    void SeleccionarCableActual()
    {
        for (int i = 0; i < cables.Length; i++)
        {
            if (cables[i] == null) continue;

            cables[i].finalCable.color = (i == _indiceActual)
                ? colorSeleccion
                : cables[i].colorOriginal;
        }
    }

    void DesresaltarTodos()
    {
        for (int i = 0; i < cables.Length; i++)
        {
            if (cables[i] == null) continue;
            cables[i].finalCable.color = cables[i].colorOriginal;
        }
    }

    // ─── Input del nuevo Input System ──────────────────────────────────────────

    float LeerVertical()
    {
        float v = 0f;
        if (Gamepad.current != null)
        {
            v = Gamepad.current.leftStick.ReadValue().y;
            // También el d-pad
            if (Gamepad.current.dpad.up.isPressed)   v =  1;
            if (Gamepad.current.dpad.down.isPressed) v = -1;
        }
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)   v =  1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v = -1;
        }
        return v;
    }

    bool BotonAccionPulsado()
    {
        bool pulsado = false;
        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) // cuadrado PS / X Xbox
            pulsado = true;
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            pulsado = true;
        return pulsado;
    }
}