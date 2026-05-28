using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem; 


public class MenuOpciones : MonoBehaviour
{
    private AudioManager audioManager;
    public Slider musicSlider;
    public Slider sfxSlider;

    public Toggle muteToggle;
    private float savedMusicVolume = 1f;
    private float savedSFXVolume = 1f;
    public TMP_Dropdown resolutionDropdown;

    private InputAction moveAction;
    private InputAction submitAction;

    void Awake()
    {
        if (PlayerInput.all.Count > 0)
        {
            var playerInput = PlayerInput.all[0];
            moveAction = playerInput.actions.FindAction("Navigate");
            submitAction = playerInput.actions.FindAction("Submit");
        }
        else
        {
            var uiModule = FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (uiModule != null)
            {
                moveAction = uiModule.move.action;
                submitAction = uiModule.submit.action;
            }
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioManager = FindFirstObjectByType<AudioManager>();
        if (audioManager != null)
        {
            musicSlider.onValueChanged.AddListener(audioManager.SetMusicVolume);
            sfxSlider.onValueChanged.AddListener(audioManager.SetSFXVolume);

            musicSlider.value = audioManager.musicSource.volume;

            muteToggle.onValueChanged.AddListener(ToggleMute);
        }
        else
        {
            Debug.LogWarning("AudioManager no encontrado en la escena.");
        }

        List<string> resolutions = new List<string> { "1920x1080", "1280x720", "800x600" };
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(resolutions);

        resolutionDropdown.onValueChanged.AddListener(ChangeResolution);

        if (PlayerPrefs.HasKey("ResolutionIndex"))
        {
            int savedIndex = PlayerPrefs.GetInt("ResolutionIndex");
            resolutionDropdown.value = savedIndex;
            ChangeResolution(savedIndex);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (moveAction == null) return;
        Vector2 navigationInput = moveAction.ReadValue<Vector2>();

        if (musicSlider.gameObject.activeSelf && EventSystem.current.currentSelectedGameObject == musicSlider.gameObject)
        {
            musicSlider.value += navigationInput.x * Time.deltaTime;
        }

        if (resolutionDropdown.gameObject.activeSelf && EventSystem.current.currentSelectedGameObject == resolutionDropdown.gameObject)
        {
            if (moveAction.triggered)
            {
                if (navigationInput.y > 0.5f)
                {
                    resolutionDropdown.value = Mathf.Max(resolutionDropdown.value - 1, 0);
                }
                else if (navigationInput.y < -0.5f)
                {
                    resolutionDropdown.value = Mathf.Min(resolutionDropdown.value + 1, resolutionDropdown.options.Count - 1);
                }
            }
        }
        
        if (muteToggle.gameObject.activeSelf && EventSystem.current.currentSelectedGameObject == muteToggle.gameObject)
        {
            if (submitAction != null && submitAction.triggered)
            {
                muteToggle.isOn = !muteToggle.isOn;
                ToggleMute(muteToggle.isOn);
            }
        }
    }

    void ToggleMute(bool isMuted)
    {
        if (isMuted)
        {
            savedMusicVolume = audioManager.musicSource.volume;
            savedSFXVolume = audioManager.sfxSource.volume;

            audioManager.musicSource.volume = 0;
            audioManager.sfxSource.volume = 0;
        }
        else
        {
            audioManager.musicSource.volume = savedMusicVolume;
            audioManager.sfxSource.volume = savedSFXVolume;
        }
    }
    
        void ChangeResolution(int index)
    {
        switch (index)
        {
            case 0: Screen.SetResolution(1920, 1080, Screen.fullScreen); break;
            case 1: Screen.SetResolution(1280, 720, Screen.fullScreen); break;
            case 2: Screen.SetResolution(800, 600, Screen.fullScreen); break;
        }

        PlayerPrefs.SetInt("ResolutionIndex", index); // Guardar configuración
    }
}
