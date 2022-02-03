using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
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
    public AnimationCurve accelerationCurve = new AnimationCurve(new Keyframe(0, 0,0,1, 0,0.25f), new Keyframe(1, 1, 1, 0, 0.25f, 0));
    public AnimationCurve decelerationCurve = new AnimationCurve(new Keyframe(-1, 1,0,-1, 0,0.25f), new Keyframe(0, 0, -1, 0, 0.25f, 0));
    public AnimationCurve inverseAccelerationCurve;
    public AnimationCurve inverseDecelerationCurve;

    [Header("Jumping")]
    public float maxJumpHeight = 5;
    public float timeToJumpApex = 0.5f;
    public int maxJumps = 2;
    private int availableJumps;
    private bool fastFall;
    public float gravityMultiplier = 3;
    public float maxFallSpeed = -50;

    public float coyoteTime = 0.1f;
    private bool isCoyoteTime;
    private float timeSinceLeftGround;

    public float jumpBufferTime = 0.1f;
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
    private BoxCollider2D boxCollider;
    private Vector2 groundCheckPosition;
    private Vector2 groundCheckSize;
    private bool isGrounded;

    [Header("Other")]
    public Transform xpostrail;
    public Transform xvelrail;


    static AnimationCurve InverseIncreasingCurve(AnimationCurve curve)
    {
        var reverse = new AnimationCurve();
        for (int i = 0; i < curve.keys.Length; i++)
        {
            var keyframe = curve.keys[i];
            var reverseKeyframe = new Keyframe();
            reverseKeyframe.weightedMode = WeightedMode.Both;
            
            reverseKeyframe.value = keyframe.time;
            reverseKeyframe.time = keyframe.value;
            
            reverseKeyframe.inTangent = 1 / keyframe.inTangent;
            reverseKeyframe.outTangent = 1 / keyframe.outTangent;
            
            reverse.AddKey(reverseKeyframe);
        }
        for (int i = 0; i < curve.keys.Length; i++)
        {
            Keyframe[] reverseKeyframes = reverse.keys;
            
            if (i != 0)
            {
                float distToPrevious = curve.keys[i].time - curve.keys[i - 1].time;
                float inHandleLengthX = curve.keys[i].inWeight * distToPrevious;
                float inHandleLengthY = -curve.keys[i].inTangent * inHandleLengthX;

                float reverseInHandleLengthX = inHandleLengthY;

                float reverseDistToPrevious = reverseKeyframes[i].time - reverseKeyframes[i - 1].time;
                reverseKeyframes[i].inWeight = -reverseInHandleLengthX / reverseDistToPrevious;
            }

            if (i != curve.keys.Length - 1)
            {
                float distToNext = curve.keys[i + 1].time - curve.keys[i].time;
                float outHandleLengthX = curve.keys[i].outWeight * distToNext;
                float outHandleLengthY = curve.keys[i].outTangent * outHandleLengthX;

                float reverseOutHandleLengthX = outHandleLengthY;

                float reverseDistToNext = reverseKeyframes[i + 1].time - reverseKeyframes[i].time;
                reverseKeyframes[i].outWeight = reverseOutHandleLengthX / reverseDistToNext;
            }

            reverse.keys = reverseKeyframes;
        }

        return reverse;
    }

    static AnimationCurve InverseDecreasingCurve(AnimationCurve curve)
    {
        var reverse = new AnimationCurve();
        for (int i = 0; i < curve.keys.Length; i++)
        {
            var keyframe = curve.keys[i];
            var reverseKeyframe = new Keyframe();
            reverseKeyframe.weightedMode = WeightedMode.Both;
            
            reverseKeyframe.value = -keyframe.time;
            reverseKeyframe.time = -keyframe.value;
            
            reverseKeyframe.inTangent = 1 / keyframe.inTangent;
            reverseKeyframe.outTangent = 1 / keyframe.outTangent;
            
            reverse.AddKey(reverseKeyframe);
        }
        for (int i = 0; i < curve.keys.Length; i++)
        {
            Keyframe[] reverseKeyframes = reverse.keys;
            
            if (i != 0)
            {
                float distToPrevious = curve.keys[i].time - curve.keys[i - 1].time;
                float inHandleLengthX = curve.keys[i].inWeight * distToPrevious;
                float inHandleLengthY = curve.keys[i].inTangent * inHandleLengthX;

                float reverseInHandleLengthX = -inHandleLengthY;

                float reverseDistToPrevious = reverseKeyframes[i].time - reverseKeyframes[i - 1].time;
                reverseKeyframes[i].inWeight = reverseInHandleLengthX / reverseDistToPrevious;
            }

            if (i != curve.keys.Length - 1)
            {
                float distToNext = curve.keys[i + 1].time - curve.keys[i].time;
                float outHandleLengthX = curve.keys[i].outWeight * distToNext;
                float outHandleLengthY = curve.keys[i].outTangent * outHandleLengthX;

                float reverseOutHandleLengthX = -outHandleLengthY;

                float reverseDistToNext = reverseKeyframes[i + 1].time - reverseKeyframes[i].time;
                reverseKeyframes[i].outWeight = reverseOutHandleLengthX / reverseDistToNext;
            }

            reverse.keys = reverseKeyframes;
        }

        return reverse;
    }


    private void Start()
    {
        inverseAccelerationCurve = InverseIncreasingCurve(accelerationCurve);
        inverseDecelerationCurve = InverseDecreasingCurve(decelerationCurve);
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

    private bool jumpButtonHolding;
    public void HandleJump(float value)
    {
        if (value > 0)
        {
            jumpButtonHolding = true;
            timeSinceJumpPress = 0;
        }
        else
        {
            jumpButtonHolding = false;
            fastFall = true;
        }
    }


    public void Jump()
    {
        Vector2 velocity = rb.velocity;
        velocity.y = (2 * maxJumpHeight) / timeToJumpApex;
        rb.velocity = velocity;

        fastFall = !jumpButtonHolding;
        inAirFromJumping = true;
        inAirFromFalling = false;
        timeSinceLastJump = 0;
    }

    public void AirJump()
    {
        availableJumps--;
        Jump();
    }

    private float t;

    private void FixedUpdate()
    {
        // print("AAAAAAAAAAAAAAAAAAAAAAAAA");
        //print("TIME:" + accelerationCurve.keys[1].time + "  " + "VALUE:" + accelerationCurve.keys[1].value + "  " + "IN-T:" + accelerationCurve.keys[1].inTangent + "  " + "IN-W:" + accelerationCurve.keys[1].inWeight + "  " + "OUT-T:" + accelerationCurve.keys[1].outTangent + "  " + "OUT-W:" + accelerationCurve.keys[1].outWeight);
        inverseAccelerationCurve = InverseIncreasingCurve(accelerationCurve);
        //print("TIME:" + inverseAccelerationCurve.keys[1].time + "  " + "VALUE:" + inverseAccelerationCurve.keys[1].value + "  " + "IN-T:" + inverseAccelerationCurve.keys[1].inTangent + "  " + "IN-W:" + inverseAccelerationCurve.keys[1].inWeight + "  " + "OUT-T:" + inverseAccelerationCurve.keys[1].outTangent + "  " + "OUT-W:" + inverseAccelerationCurve.keys[1].outWeight);
        
        // print("AAAAAAAAAAAAAAAAAAAAAAAAA");
        //print("TIME:" + decelerationCurve.keys[1].time + "  " + "VALUE:" + decelerationCurve.keys[1].value + "  " + "IN-T:" + decelerationCurve.keys[1].inTangent + "  " + "IN-W:" + decelerationCurve.keys[1].inWeight + "  " + "OUT-T:" + decelerationCurve.keys[1].outTangent + "  " + "OUT-W:" + decelerationCurve.keys[1].outWeight);
        inverseDecelerationCurve = InverseDecreasingCurve(decelerationCurve);
        //print("TIME:" + inverseDecelerationCurve.keys[1].time + "  " + "VALUE:" + inverseDecelerationCurve.keys[1].value + "  " + "IN-T:" + inverseDecelerationCurve.keys[1].inTangent + "  " + "IN-W:" + inverseDecelerationCurve.keys[1].inWeight + "  " + "OUT-T:" + inverseDecelerationCurve.keys[1].outTangent + "  " + "OUT-W:" + inverseDecelerationCurve.keys[1].outWeight);

        
        //Jumping Logic
        bool wasGrounded = isGrounded;
        if (rb.velocity.y > 0)
            isGrounded = false;
        else
            isGrounded = Physics2D.BoxCast((Vector2) transform.position + groundCheckPosition, groundCheckSize, 0, Vector2.down, 0, groundMask);

        if (isGrounded)
        {

        }

        //first frame landing on ground
        if (!wasGrounded && isGrounded)
        {
            fastFall = true;
            timeSinceLeftGround = 0;
            inAirFromJumping = false;
            inAirFromFalling = false;
            availableJumps = maxJumps;
        }

        //first frame leaving ground
        if (wasGrounded && !isGrounded)
        {
            availableJumps--;

            if (jumpInCooldown)
            {
                inAirFromJumping = true;
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
        float percentSpeed = Mathf.Abs(velocity.x) / maxSpeed;
        bool a = moveInputDirection.x < -0.01f && velocity.x <= 0.1f;
        bool b = moveInputDirection.x > 0.01f && velocity.x >= -0.1f;
        if (a || b) //accelerate
        {
            if (isGrounded)
                percentSpeed = Mathf.Clamp01(inverseAccelerationCurve.Evaluate(percentSpeed) + ((1 / timeToMaxSpeed) * Time.fixedDeltaTime));
            else
                percentSpeed = Mathf.Clamp01(inverseAccelerationCurve.Evaluate(percentSpeed) + ((1 / timeToMaxSpeed) * Time.fixedDeltaTime) * inAirAccelerationMultiplier);
            
            velocity.x = maxSpeed * accelerationCurve.Evaluate(percentSpeed) * Mathf.Sign(moveInputDirection.x);
        }
        else //decelerate
        {
            if (isGrounded)
                percentSpeed = Mathf.Clamp01(inverseDecelerationCurve.Evaluate(-percentSpeed) - ((1 / timeToStop) * Time.fixedDeltaTime));
            else
                percentSpeed = Mathf.Clamp01(inverseDecelerationCurve.Evaluate(-percentSpeed) - ((1 / timeToStop) * Time.fixedDeltaTime) * inAirDecelerationMultiplier);
            
            velocity.x = maxSpeed * decelerationCurve.Evaluate(-percentSpeed) * Mathf.Sign(velocity.x);
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