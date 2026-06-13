using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
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

    [Header("Sensibilidad de cámara")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText;
    public float minSensitivity = 0.3f;
    public float maxSensitivity = 3f;
    public float defaultSensitivity = 1f;
    private const string SENS_PREF_KEY = "CameraSensitivity";

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

    void Start()
    {
        audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            float defaultMusicVolume = 0.3f;

            volumeSlider.onValueChanged.AddListener(audioManager.SetMusicVolume);
            sfxVolumeSlider.onValueChanged.AddListener(audioManager.SetSFXVolume);

            float volM, volS;
            audioManager.mainMixer.GetFloat("MusicVol", out volM);
            audioManager.mainMixer.GetFloat("SFXVol", out volS);

            volumeSlider.value = Mathf.Pow(10, volM / 20);
            sfxVolumeSlider.value = Mathf.Pow(10, volS / 20);
        }
        else
        {
            Debug.LogWarning("AudioManager no encontrado en la escena.");
        }

        // Configurar slider de sensibilidad
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = minSensitivity;
            sensitivitySlider.maxValue = maxSensitivity;
            sensitivitySlider.value = PlayerPrefs.GetFloat(SENS_PREF_KEY, defaultSensitivity);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            UpdateSensitivityText(sensitivitySlider.value);
        }

        pauseMenu.SetActive(false);
    }

    void Update()
    {
        if (isPaused && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            // Detectar qué slider está seleccionado para mover con D-pad
            Slider targetSlider = null;

            if (selected == volumeSlider.gameObject)
                targetSlider = volumeSlider;
            else if (selected == sfxVolumeSlider.gameObject)
                targetSlider = sfxVolumeSlider;
            else if (sensitivitySlider != null && selected == sensitivitySlider.gameObject)
                targetSlider = sensitivitySlider;

            if (targetSlider != null)
            {
                Vector2 movement = moveAction.action.ReadValue<Vector2>();
                float horizontalInput = movement.x;

                if (Mathf.Abs(horizontalInput) > 0.1f)
                {
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

        pauseMenu.SetActive(isPaused);

        Time.timeScale = isPaused ? 0 : 1;
        Cursor.visible = true;

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

    // ─── Sensibilidad ────────────────────────────────────────────────────────

    private void OnSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat(SENS_PREF_KEY, value);
        PlayerPrefs.Save();
        UpdateSensitivityText(value);
    }

    private void UpdateSensitivityText(float value)
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("F1");
    }

    public void ResetSensitivity()
    {
        if (sensitivitySlider != null)
            sensitivitySlider.value = defaultSensitivity;
    }

    // ─── Botones ─────────────────────────────────────────────────────────────

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