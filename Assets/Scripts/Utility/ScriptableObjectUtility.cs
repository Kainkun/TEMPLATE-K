using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "ScriptableObjectUtility", menuName = "TEMPLATE-K/ScriptableObjectUtility", order = 1)]
public class ScriptableObjectUtility : ScriptableObject
{
    public void ToggleCreditsUI() => GameManager.Get().ToggleCreditsUI();
    public void ToggleCreditsUI(bool active) => GameManager.Get().ToggleCreditsUI(active);
    
    public void TogglePauseUI() => GameManager.Get().TogglePauseUI();
    public void TogglePauseUI(bool active) => GameManager.Get().TogglePauseUI(active);
    
    public void ToggleSettingsUI() => GameManager.Get().ToggleSettingsUI();
    public void ToggleSettingsUI(bool active) => GameManager.Get().ToggleSettingsUI(active);

    public void Unpause() => InputManager.Get().OnUnpause();
    
    
    public void LoadScene(int index) => GameManager.Get().LoadScene(index);

    public void PlaySound(AudioClip audioClip) => AudioManager.Get().PlaySound(audioClip);
    public void PlaySound(AudioClip audioClip, float volume) => AudioManager.Get().PlaySound(audioClip, volume);

    public void QuitGame() => GameManager.Get().QuitGame();
}