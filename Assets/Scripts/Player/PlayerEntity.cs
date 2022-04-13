using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEntity : Entity
{
    private PlatformerController _platformerController;
    
    private void Start()
    {
        _platformerController = GetComponent<PlatformerController>();
        _platformerController.onCrushed += Die;
    }
}