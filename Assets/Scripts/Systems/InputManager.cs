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


    // public InputActionMap[] maps;
    // public InputActionAsset actions;
    
    private void Start()
    {
        SceneManager.sceneLoaded += HandleLoadScene;

        // maps = playerInput.actions.actionMaps.ToArray();
        // print(playerInput.defaultActionMap);
        // print(playerInput.actions);
        // foreach (InputActionMap inputActionMap in playerInput.actions.actionMaps)
        // {
        //     print(inputActionMap.name);
        // }


        // inputslist.IndexOf("Player");
        //
        //
        // inputStack.Add(inputslist.IndexOf("Player"));
        //
        // _inputPriority.Add(new Object(), "test4");
        // _inputPriority.Add(new Object(), "test3");
        // _inputPriority.Add(new Object(), "test2");
        // _inputPriority.Add(new Object(), "test222");
        // _inputPriority.Add(new Object(), "test5");
        // _inputPriority.Add(new Object(), "test1");
        // for (int i = 0; i < _inputPriority.Count; i++)
        // {
        //     print(_inputPriority.Values[i]);
        // }
        // print(_inputPriority.Remove(new Object()));
    }

    // public bool AddInput(string actionMapName, int priority)
    // {
    //     if (priority < _inputPriority.Keys[_inputPriority.Count])
    //         return false;
    //     
    //     _inputPriority.Add(priority, actionMapName);
    //     playerInput.SwitchCurrentActionMap(_inputPriority.Values[_inputPriority.Count]);
    //
    //     return true;
    // }

    // public void RemoveInput(string actionMapName)
    // {
    //     _inputPriority.IndexOfValue("test4");
    //     playerInput.SwitchCurrentActionMap(_inputPriority.Values[_inputPriority.Count]);
    // }

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
