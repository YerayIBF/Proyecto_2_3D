using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioMixer mainMixer;
    public static AudioManager Instance;
    public Sound[] musicSounds,sfxSounds;
    public AudioSource musicSource,sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {

    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Escena cargada: " + scene.name); 

        if (scene.name == "Menu")
        {
            PlayMusic("Menu");
        }
        else if (scene.name == "nombreEscenaJuego")
        {
            PlayMusic("nombreEscenaJuego");
        }
    }
    public void PlayMusic(string name)
    {
        Sound sound = Array.Find(musicSounds, s => s.soundName == name);

        if (sound == null)
        {
            Debug.Log("No Sound");
        }
        else
        {
            musicSource.clip = sound.clip;
            musicSource.Play();
        }
    }

    public void PlaySFX(string name)
    {
        Sound sfx = Array.Find(sfxSounds, s => s.soundName == name);

        if (sfx == null)
        {
            Debug.Log("No SFX");
        }
        else
        {
            sfxSource.PlayOneShot(sfx.clip,0.5f);
        }
    }


    
    public void Stop()
    {
        musicSource.Stop();
        sfxSource.Stop();
    }

        public void SetMusicVolume(float volume)
    {
        mainMixer.SetFloat("MusicaVol", Mathf.Log10(volume) * 20);
    }

        public void SetSFXVolume(float volume)
    {
        mainMixer.SetFloat("SFXVol", Mathf.Log10(volume) * 20);
    }
}
