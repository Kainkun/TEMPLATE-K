using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public class GameData
{
    public static LayerMask defaultGroundMask;
    public static LayerMask platformMask;
    public static LayerMask traversableMask;
    
    public void SetData()
    {
        defaultGroundMask = LayerMask.GetMask("Default");
        platformMask = LayerMask.GetMask("Platform");
        traversableMask =  defaultGroundMask | platformMask;
    }
}