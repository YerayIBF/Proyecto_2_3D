using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.InputSystem;

public class MenuManager : MonoBehaviour
{
    public GameObject panelOpciones;
    public GameObject panelControles;
    public GameObject panelMenu;

    public GameObject botonInicialMenu;
    public GameObject botonInicialOpciones;
    public GameObject botonInicialControles;

    private InputAction actionCancelar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        actionCancelar = new InputAction(binding: "<Gamepad>/buttonEast");
        actionCancelar.AddBinding("<Keyboard>/escape");
        actionCancelar.Enable();

        EventSystem.current.SetSelectedGameObject(botonInicialMenu);
    }

    public void Update()
    {
        if (actionCancelar.WasPressedThisFrame())
        {
            if (panelOpciones.activeSelf)
                CerrarOpciones();
            else if (panelControles.activeSelf)
                CerrarControles();
        }
    }

    public void Jugar()
    {
        //GraphicsSettings.renderPipelineAsset = Resources.Load<RenderPipelineAsset>("UniversalRenderPipelineAsset");
        //SceneManager.LoadScene("SceneRaul"); 
        Time.timeScale = 1;
    }

    public void AbrirOpciones()
    {
        panelMenu.SetActive(false);
        panelOpciones.SetActive(true); 
        EventSystem.current.SetSelectedGameObject(botonInicialOpciones);
    }

    public void CerrarOpciones()
    {
        panelMenu.SetActive(true);
        panelOpciones.SetActive(false);
        EventSystem.current.SetSelectedGameObject(botonInicialMenu); 
    }

    public void AbrirControles()
    {
        panelMenu.SetActive(false);
        panelControles.SetActive(true);
        EventSystem.current.SetSelectedGameObject(botonInicialControles); 
    }

    public void CerrarControles()
    {
        panelMenu.SetActive(true);
        panelControles.SetActive(false); 
        EventSystem.current.SetSelectedGameObject(botonInicialMenu);
    }

    public void Salir(){
        Application.Quit();
    }
}
