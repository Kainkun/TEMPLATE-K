using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InputManager : SystemSingleton<InputManager>
{
    private PlayerInput playerInput;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(transform.gameObject);
        playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        SceneManager.sceneLoaded += HandleLoadScene;
    }

    public void HandleLoadScene(Scene scene, LoadSceneMode loadSceneMode)
    {
        if(scene.buildIndex == 0)
            playerInput.SwitchCurrentActionMap("UI");
        else
            playerInput.SwitchCurrentActionMap("Player");
    }

    public void OnPlayerPause()
    {
        if(SceneManager.GetActiveScene().buildIndex != 0)
        {
            playerInput.SwitchCurrentActionMap("UI");
            GameManager.Get().TogglePauseUI(true);
            GameManager.Get().TogglePause(true);
        }
    }

    public void OnUnpause()
    {
        if(SceneManager.GetActiveScene().buildIndex != 0)
        {
            playerInput.SwitchCurrentActionMap("Player");
            GameManager.Get().TogglePauseUI(false);
            GameManager.Get().TogglePause(false);
        }
    }

    public void OnBack()
    {
        var gm = GameManager.Get();
        
        if (gm.overlaySettings.activeSelf)
        {
            gm.ToggleSettingsUI(false);
        }
        else if(gm.overlayPause.activeSelf)
        {
            OnUnpause();
        }
        else if (gm.overlayCredits.activeSelf)
        {
            gm.ToggleCreditsUI(false);
        }
    }

    public Action<float> Jump;
    public void OnJump(InputValue value)
    {
        //print(value.Get<float>());
        Jump?.Invoke(value.Get<float>());
    }

    public Action<Vector2> Move;
    public void OnMove(InputValue value)
    {
        //print(value.Get<Vector2>());
        Move?.Invoke(value.Get<Vector2>());
    }

    public Action<Vector2> Look;
    public void OnLook(InputValue value)
    {
        //print(value.Get<Vector2>());
        Look?.Invoke(value.Get<Vector2>());
    }
}
