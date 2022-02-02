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
    public float maxSpeed = 10;
    public float timeToMaxSpeed = 0.5f;
    public float timeToStop = 0.2f;
    public float inAirAccelerationMultiplier = 0.5f;
    public float inAirDecelerationMultiplier = 0.5f;
    private Vector2 moveInputDirection;
    public AnimationCurve accelerationCurve = new AnimationCurve(new Keyframe(0, 0,0,1, 0,0.25f), new Keyframe(1, 1, 1, 0, 0.25f, 0));
    public AnimationCurve decelerationCurve = new AnimationCurve(new Keyframe(0, 1,0,-1, 0,0.25f), new Keyframe(1, 0, -1, 0, 0.25f, 0));

    [Header("Jumping")]
    public float maxJumpHeight = 5;
    public float timeToJumpApex = 0.5f;
    public int maxJumps = 2;
    private int availableJumps;
    private bool fastFall;
    public float gravityMultiplier = 3;
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
    public float groundCheckThickness = 0.2f;
    public LayerMask groundMask;
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
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
        boxCollider = GetComponent<BoxCollider2D>();

        groundCheckPosition = new Vector2(0, -boxCollider.size.y / 2);
        groundCheckSize = new Vector2(boxCollider.size.x, groundCheckThickness);

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

    private float momentumPercentage = 0;

    private void FixedUpdate()
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
            timeSinceLeftGround += Time.fixedDeltaTime;
            isCoyoteTime = timeSinceLeftGround < coyoteTime;
        }
        else
        {
            isCoyoteTime = false;
        }

        timeSinceJumpPress += Time.fixedDeltaTime;
        waitingForJump = timeSinceJumpPress < jumpBufferTime;

        timeSinceLastJump += Time.fixedDeltaTime;
        jumpInCooldown = timeSinceLastJump < jumpCooldownTime;


        Vector2 velocity = rb.velocity;

        //Gravity
        if (velocity.y < 0)
            fastFall = true;
        float gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2) * Time.fixedDeltaTime;
        if (fastFall)
            gravity *= gravityMultiplier;
        velocity.y += gravity;
        velocity.y = Mathf.Max(velocity.y, maxFallSpeed);

        //Movement
        float percentMaxSpeed = Mathf.Abs(velocity.x) / maxSpeed;
        float targetVelocity = moveInputDirection.x * maxSpeed;
        bool a = moveInputDirection.x < -0.01f && velocity.x <= 0;
        bool b = moveInputDirection.x > 0.01f && velocity.x >= 0;

        if (a || b)
        {
            if (isGrounded)
                momentumPercentage = Mathf.Clamp01(momentumPercentage + ((1 / timeToMaxSpeed) * Time.fixedDeltaTime));
            else
                momentumPercentage = Mathf.Clamp01(momentumPercentage + ((1 / timeToMaxSpeed) * Time.fixedDeltaTime) * inAirAccelerationMultiplier);
            velocity.x = maxSpeed * accelerationCurve.Evaluate(momentumPercentage) * Mathf.Sign(moveInputDirection.x);
        }
        else //decelerate
        {
            if (isGrounded)
                momentumPercentage = Mathf.Clamp01(momentumPercentage - ((1 / timeToStop) * Time.fixedDeltaTime));
            else
                momentumPercentage = Mathf.Clamp01(momentumPercentage - ((1 / timeToStop) * Time.fixedDeltaTime) * inAirDecelerationMultiplier);
            velocity.x = maxSpeed * decelerationCurve.Evaluate(1 - momentumPercentage) * Mathf.Sign(velocity.x); //wall bump issue
        }
        rb.velocity = velocity;

        //Debug trails
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