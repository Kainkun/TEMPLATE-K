using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "ScriptableObjectUtility", menuName = "TEMPLATE-K/ScriptableObjectUtility", order = 1)]
public class ScriptableObjectUtility : ScriptableObject
{
    public void ToggleCredits() => GameManager.Get().ToggleCredits();
    public void ToggleCredits(bool active) => GameManager.Get().ToggleCredits(active);
    
    public void TogglePause() => GameManager.Get().TogglePause();
    public void TogglePause(bool active) => GameManager.Get().TogglePause(active);
    
    public void ToggleSettings() => GameManager.Get().ToggleSettings();
    public void ToggleSettings(bool active) => GameManager.Get().ToggleSettings(active);
    
    
    public void LoadScene(int index) => GameManager.Get().LoadScene(index);

    public void PlaySound(AudioClip audioClip) => AudioManager.Get().PlaySound(audioClip);
    public void PlaySound(AudioClip audioClip, float volume) => AudioManager.Get().PlaySound(audioClip, volume);

    public void QuitGame() => GameManager.Get().QuitGame();
}