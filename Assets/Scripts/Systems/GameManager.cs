using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameManager : SystemSingleton<GameManager>
{
    [HideInInspector] public GameObject overlayCredits;
    [HideInInspector] public GameObject overlayPause;
    [HideInInspector] public GameObject overlaySettings;
    [HideInInspector] public GameObject eventSystem;
    
    public static bool applicationIsQuitting = false;

    private void Start()
    {
        DontDestroyOnLoad(transform.gameObject);
        
        Application.quitting += () => applicationIsQuitting = true;

        overlayCredits = Instantiate(Resources.Load<GameObject>("Overlay Credits"));
        overlayPause = Instantiate(Resources.Load<GameObject>("Overlay Pause"));
        overlaySettings = Instantiate(Resources.Load<GameObject>("Overlay Settings"));
        eventSystem = Instantiate(Resources.Load<GameObject>("EventSystem"));
        
         DontDestroyOnLoad(overlayCredits);
         DontDestroyOnLoad(overlayPause);
         DontDestroyOnLoad(overlaySettings);
         DontDestroyOnLoad(eventSystem);
        
         overlayCredits.SetActive(false);
         overlayPause.SetActive(false);
         overlaySettings.SetActive(false);
    }

    public void ToggleCreditsUI() => overlayCredits.SetActive(!overlayCredits.activeSelf);
    public void ToggleCreditsUI(bool active) =>overlayCredits.SetActive(active);
    public void TogglePauseUI() => overlayPause.SetActive(!overlayPause.activeSelf);
    public void TogglePauseUI(bool active) =>overlayPause.SetActive(active);
    public void ToggleSettingsUI() => overlaySettings.SetActive(!overlaySettings.activeSelf);
    public void ToggleSettingsUI(bool active) =>overlaySettings.SetActive(active);


    
    [HideInInspector] public bool paused;
    public Action<bool> OnPauseChange;
    public void TogglePause()
    {
        TogglePause(!paused);
    }
    
    public void TogglePause(bool pause)
    {
        paused = pause;
        Time.timeScale = pause ? 0 : 1;
        OnPauseChange?.Invoke(pause);
    }
    
    
    public void LoadScene(int index)
    {
        SceneManager.LoadScene(index);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif !UNITY_WEBGL
         Application.Quit();
#endif
    }
}