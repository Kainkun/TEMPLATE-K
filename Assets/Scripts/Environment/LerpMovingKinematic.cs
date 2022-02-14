using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class LerpMovingKinematic : MonoBehaviour
{
    private MovingKinematic _movingKinematic;
    private bool forward = true;
    private float t = 0;
    public float forwardTime = 1;
    public float backTime = 1;
    public Vector2 direction = new Vector2(5, 0);

    private void Start()
    {
        _movingKinematic = GetComponent<MovingKinematic>();
    }

    private void FixedUpdate()
    {
        if (forwardTime <= 0 || backTime <= 0)
            return;
        
        if (forward)
        {
            t += Time.fixedDeltaTime * 1 / forwardTime;
            t = Mathf.Clamp01(t);
            
            if (t >= 1)
                forward = false;
        }
        else
        {
            t -= Time.fixedDeltaTime * 1 / backTime;
            t = Mathf.Clamp01(t);
            
            if (t <= 0)
                forward = true;
        }
        
        var position = Vector2.Lerp(_movingKinematic.StartPosition, _movingKinematic.StartPosition + direction, t);
        _movingKinematic.MovementUpdate(position);
    }
}