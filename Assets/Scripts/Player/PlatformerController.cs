using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlatformerController : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 15;
    public float timeToMaxSpeed = 0.5f;
    public float timeToStop = 0.2f;
    public float inAirAccelerationMultiplier = 0.5f;
    public float inAirDecelerationMultiplier = 0.5f;
    private Vector2 moveInputDirection;

    [Header("Jumping")]
    public float maxJumpHeight = 6;
    public float timeToJumpApex = 0.5f;
    public int maxJumps = 2;
    private int availableJumps;
    private bool fastFall;
    public float gravityMultiplier = 2;
    public float maxFallSpeed = -50;

    public float coyoteTime = 0.2f;
    private bool isCoyoteTime;
    private float timeSinceLeftGround;

    public float jumpBufferTime = 0.2f;
    private bool waitingForJump;
    private float timeSinceJumpPress;

    public float jumpCooldownTime = 0.2f;
    private bool jumpInCooldown;
    private float timeSinceLastJump;

    private bool inAirFromJumping;
    private bool inAirFromFalling;

    [Header("Physics")]
    public float groundCheckThickness = 0.1f;
    public LayerMask groundMask = 1;
    private Rigidbody2D rb;
    private BoxCollider2D collider;
    private Vector2 groundCheckPosition;
    private Vector2 groundCheckSize;
    private bool isGrounded;

    [Header("Other")]
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

        timeSinceJumpPress = jumpCooldownTime;
        timeSinceLastJump = jumpCooldownTime;
    }

    private void OnDestroy()
    {
        if (GameManager.applicationIsQuitting)
            return;
        InputManager.Get().Jump -= HandleJump;
        InputManager.Get().Move -= HandleMove;
        InputManager.Get().Look -= HandleLook;
    }


    public void HandleMove(Vector2 value)
    {
        moveInputDirection = value;
    }

    public void HandleLook(Vector2 value)
    {
        //print(value);
    }

    public void HandleJump(float value)
    {
        if (value > 0)
            timeSinceJumpPress = 0;
        else
            fastFall = true;
    }


    public void Jump()
    {
        Vector2 velocity = rb.velocity;
        velocity.y = (2 * maxJumpHeight) / timeToJumpApex;
        rb.velocity = velocity;

        fastFall = false;
        inAirFromJumping = true;
        inAirFromFalling = false;
        timeSinceLastJump = 0;
    }

    public void AirJump()
    {
        availableJumps--;
        Jump();
    }

    private void Update()
    {
        //Jumping Logic
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.BoxCast((Vector2) transform.position + groundCheckPosition, groundCheckSize, 0, Vector2.down, 0, groundMask);

        if (isGrounded)
        {
            fastFall = true;
            timeSinceLeftGround = 0;
            inAirFromJumping = false;
            inAirFromFalling = false;
            availableJumps = maxJumps;
        }

        //first frame landing on ground
        if (!wasGrounded && isGrounded)
        {
            
        }

        //first frame leaving ground
        if (wasGrounded && !isGrounded)
        {
            availableJumps--;

            if (jumpInCooldown)
            {
                inAirFromJumping = true;
                fastFall = false;
            }
            else
                inAirFromFalling = true;
        }

        if (waitingForJump && !jumpInCooldown)
        {
            if (isGrounded || isCoyoteTime)
                Jump();
            else if (availableJumps > 0)
                AirJump();
        }

        if (inAirFromFalling)
        {
            timeSinceLeftGround += Time.deltaTime;
            isCoyoteTime = timeSinceLeftGround < coyoteTime;
        }
        else
        {
            isCoyoteTime = false;
        }

        timeSinceJumpPress += Time.deltaTime;
        waitingForJump = timeSinceJumpPress < jumpBufferTime;

        timeSinceLastJump += Time.deltaTime;
        jumpInCooldown = timeSinceLastJump < jumpCooldownTime;


        Vector2 velocity = rb.velocity;

        //Gravity
        if (velocity.y < 0)
            fastFall = true;
        float gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2) * Time.deltaTime;
        if (fastFall)
            gravity *= gravityMultiplier;
        velocity.y += gravity;
        velocity.y = Mathf.Max(velocity.y, maxFallSpeed);

        //Movement
        float targetVelocity = moveInputDirection.x * maxSpeed;
        float newVelocity;
        bool a = moveInputDirection.x < -0.01f && velocity.x <= 0;
        bool b = moveInputDirection.x > 0.01f && velocity.x >= 0;
        if (a || b)
        {
            if (isGrounded)
                newVelocity = Mathf.MoveTowards(velocity.x, targetVelocity, maxSpeed * (1 / timeToMaxSpeed) * Time.deltaTime);
            else
                newVelocity = Mathf.MoveTowards(velocity.x, targetVelocity, maxSpeed * (1 / timeToMaxSpeed) * Time.deltaTime * inAirAccelerationMultiplier);
        }
        else
        {
            if (isGrounded)
                newVelocity = Mathf.MoveTowards(velocity.x, 0, maxSpeed * (1 / timeToStop) * Time.deltaTime);
            else
                newVelocity = Mathf.MoveTowards(velocity.x, 0, maxSpeed * (1 / timeToStop) * Time.deltaTime * inAirDecelerationMultiplier);
        }
        velocity.x = newVelocity;
        rb.velocity = velocity;


        Vector2 v = xpostrail.position;
        v.x += Time.deltaTime;
        v.y = transform.position.x;
        xpostrail.position = v;
        v = xvelrail.position;
        v.x += Time.deltaTime;
        v.y = rb.velocity.x;
        xvelrail.position = v;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + (Vector3) groundCheckPosition, groundCheckSize);
    }
}