using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlatformerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D collider;
    private Vector2 moveInputDirection;

    public float groundCheckThickness = 0.2f;
    private Vector2 groundCheckPosition;
    private Vector2 groundCheckSize;
    public LayerMask ground;
    public bool isGrounded;
    public int maxJumps = 2;
    private int availableJumps;
    private bool holdingJump;
    private bool fastFall;

    public float maxSpeed = 10;
    public float timeToMaxSpeed = 0.5f;
    public float timeToStop = 0.2f;

    public float maxJumpHeight = 5;
    public float timeToJumpApex = 0.5f;

    private bool isCoyoteTime;
    public float coyoteTime = 0.2f;
    public float coyoteTimeCounter;
    private Coroutine CR_jumpBuffer;
    public float jumpBuffer = 0.2f;
    public float jumpBufferCounter;
    private bool isJumpCooldown;
    public float jumpCooldown = 0.2f;
    public float jumpCooldownCounter;


    public float fallMultiplier = 3;
    public float maxFallSpeed = -50;

    public Transform xpostrail;
    public Transform xvelrail;


    private void Start()
    {
        InputManager.Get().Jump += HandleJump;
        InputManager.Get().Move += HandleMove;
        InputManager.Get().Look += HandleLook;

        availableJumps = maxJumps;

        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<BoxCollider2D>();

        groundCheckPosition = new Vector2(0, -collider.size.y / 2);
        groundCheckSize = new Vector2(collider.size.x, groundCheckThickness);
    }

    private void OnDestroy()
    {
        if (GameManager.applicationIsQuitting)
            return;
        InputManager.Get().Jump -= HandleJump;
        InputManager.Get().Move -= HandleMove;
        InputManager.Get().Look -= HandleLook;
    }

    IEnumerator JumpBuffer()
    {
        yield return new WaitForSeconds(jumpBuffer);
        CR_jumpBuffer = null;
    }

    public void HandleJump(float value)
    {
        holdingJump = value > 0;

        if (value > 0)
        {
            if(CR_jumpBuffer != null)
                StopCoroutine(CR_jumpBuffer);
            CR_jumpBuffer = StartCoroutine(JumpBuffer());
        }
    }

    public void TryJump()
    {
        if (availableJumps > 0 && jumpCooldownCounter >= jumpCooldown)
        {
            if(CR_jumpBuffer != null)
            {
                StopCoroutine(CR_jumpBuffer);
                CR_jumpBuffer = null;
            }

            jumpCooldownCounter = 0;
            fastFall = false;
            if (!isGrounded)
                availableJumps--;

            Vector2 velocity = rb.velocity;
            velocity.y = (2 * maxJumpHeight) / timeToJumpApex;
            rb.velocity = velocity;
        }
    }

    public void HandleMove(Vector2 value)
    {
        moveInputDirection = value;
    }

    public void HandleLook(Vector2 value)
    {
        //print(value);
    }


    private void Update()
    {
        jumpCooldownCounter += Time.deltaTime;

        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox((Vector2) transform.position + groundCheckPosition, groundCheckSize, 0, ground);
        if (isGrounded)
        {
            if (CR_jumpBuffer != null)
                TryJump();
            fastFall = false;
            availableJumps = maxJumps;
        }
        else
        {
            if (wasGrounded)
            {
                availableJumps--;
            }
        }

        //Movement
        Vector2 velocity = rb.velocity;
        float targetSpeed = moveInputDirection.x * maxSpeed;
        float newSpeed;
        bool a = moveInputDirection.x < -0.01f && velocity.x <= 0;
        bool b = moveInputDirection.x > 0.01f && velocity.x >= 0;
        if (a || b)
            newSpeed = Mathf.MoveTowards(velocity.x, targetSpeed, maxSpeed * (1 / timeToMaxSpeed) * Time.deltaTime);
        else
            newSpeed = Mathf.MoveTowards(velocity.x, 0, maxSpeed * (1 / timeToStop) * Time.deltaTime);
        velocity.x = newSpeed;

        //Gravity
        if (velocity.y < 0 || !holdingJump)
            fastFall = true;
        float gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2) * Time.deltaTime;
        if (fastFall)
            gravity *= fallMultiplier;
        velocity.y += gravity;
        velocity.y = Mathf.Max(velocity.y, maxFallSpeed);
        print(velocity.y);


        rb.velocity = velocity;


        Vector2 v = xpostrail.position;
        v.x += Time.fixedDeltaTime;
        v.y = transform.position.x;
        xpostrail.position = v;
        v = xvelrail.position;
        v.x += Time.fixedDeltaTime;
        v.y = rb.velocity.x;
        xvelrail.position = v;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + (Vector3) groundCheckPosition, groundCheckSize);
    }
}