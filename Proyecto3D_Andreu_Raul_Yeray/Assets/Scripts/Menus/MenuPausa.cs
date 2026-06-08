using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class MenuPausa : MonoBehaviour
{
    public GameObject pauseMenu;
    private bool isPaused = false;
    private AudioManager audioManager;
    public Slider volumeSlider;
    public Slider sfxVolumeSlider;

    public InputActionReference pauseAction; 
    public InputActionReference moveAction;
    public GameObject panelControls;

    void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePerformed;
        }
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
        if (moveAction != null)
        {
            moveAction.action.Disable();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            float defaultMusicVolume = 0.3f;

            //audioManager.SetMusicVolume(defaultMusicVolume);

            volumeSlider.onValueChanged.AddListener(audioManager.SetMusicVolume);
            //volumeSlider.value = audioManager.musicSource.volume;
            //volumeSlider.value = defaultMusicVolume;

            sfxVolumeSlider.onValueChanged.AddListener(audioManager.SetSFXVolume);

            float volM, volS;
            audioManager.mainMixer.GetFloat("MusicaVol", out volM);
            audioManager.mainMixer.GetFloat("SFXVol", out volS);
            //sfxVolumeSlider.value = audioManager.sfxSource.volume;

            volumeSlider.value = Mathf.Pow(10, volM / 20);
            sfxVolumeSlider.value = Mathf.Pow(10, volS / 20);
        }
        else
        {
            Debug.LogWarning("AudioManager no encontrado en la escena.");
        }
        pauseMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (isPaused && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected == volumeSlider.gameObject || selected == sfxVolumeSlider.gameObject)
            {
                Vector2 movement = moveAction.action.ReadValue<Vector2>();
                float horizontalInput = movement.x; 

                if (Mathf.Abs(horizontalInput) > 0.1f)
                {
                    Slider targetSlider = (selected == volumeSlider.gameObject) ? volumeSlider : sfxVolumeSlider;
                    targetSlider.value += horizontalInput * Time.unscaledDeltaTime * 2f;
                }
            }
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    void TogglePause()
    {
        isPaused = !isPaused;

        // Activar o desactivar el menú
        pauseMenu.SetActive(isPaused);

        // Pausar o reanudar el juego
        Time.timeScale = isPaused ? 0 : 1;
        Cursor.visible = true;
      //Cursor.visible = isPaused;
      //Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        
        if (isPaused && EventSystem.current != null)
        {
            Selectable primerElemento = pauseMenu.GetComponentInChildren<Selectable>();
            if (primerElemento != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(primerElemento.gameObject);
            }
        }
    }

    public void IrAlMenu()
    {
        SceneManager.LoadScene("Menu"); 
        Time.timeScale = 1; 
    }

    public void AbrirControles()
    {
        panelControls.SetActive(true);
        pauseMenu.SetActive(false);
        
        if (EventSystem.current != null)
        {
            Selectable botonCerrar = panelControls.GetComponentInChildren<Selectable>();
            if (botonCerrar != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(botonCerrar.gameObject);
            }
        }
    }

    public void CerrarControls()
    {
        panelControls.SetActive(false);
        pauseMenu.SetActive(true);

        Selectable primerElemento = pauseMenu.GetComponentInChildren<Selectable>();
        if (primerElemento != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(primerElemento.gameObject);
        }
    }
}
