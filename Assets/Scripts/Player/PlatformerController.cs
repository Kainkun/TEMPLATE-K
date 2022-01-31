using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 moveInputDirection;
    private void Start()
    {
        InputManager.Get().Jump += HandleJump;
        InputManager.Get().Move += HandleMove;
        InputManager.Get().Look += HandleLook;

        rb = GetComponent<Rigidbody2D>();
    }
    
    private void OnDestroy()
    {
        InputManager.Get().Jump -= HandleJump;
        InputManager.Get().Move -= HandleMove;
        InputManager.Get().Look -= HandleLook;
    }

    public void HandleJump(float value)
    {
        if(value != 0)
            rb.AddForce(Vector2.up * 10, ForceMode2D.Impulse);
    }

    public void HandleMove(Vector2 value)
    {
        moveInputDirection = value;
    }

    public void HandleLook(Vector2 value)
    {
        print(value);
    }

    public float moveSpeed = 1;
    public float acceleration = 1;
    public float deceleration = -1;
    public float velPower = 1;

    private void FixedUpdate()
    {
        float targetSpeed = moveInputDirection.x * moveSpeed;
        float speedDif = targetSpeed - rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDif) * accelRate, velPower) * Mathf.Sign(speedDif);
        rb.AddForce(movement * Vector2.right);
    }
}