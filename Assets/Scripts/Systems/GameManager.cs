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
    private GameObject overlayCredits;
    private GameObject overlayPause;
    private GameObject overlaySettings;
    private GameObject eventSystem;

    private void Start()
    {
        DontDestroyOnLoad(transform.gameObject);

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

    public void ToggleCredits() => overlayCredits.SetActive(!overlayCredits.activeSelf);
    public void ToggleCredits(bool active) =>overlayCredits.SetActive(active);
    public void TogglePause() => overlayPause.SetActive(!overlayPause.activeSelf);
    public void TogglePause(bool active) =>overlayPause.SetActive(active);
    public void ToggleSettings() => overlaySettings.SetActive(!overlaySettings.activeSelf);
    public void ToggleSettings(bool active) =>overlaySettings.SetActive(active);
    
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