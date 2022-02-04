using System;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent( typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(AudioSource) )]
public class PlatformerController : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 15;
    public float timeToMaxSpeed = 0.5f;
    public float timeToStop = 0.2f;
    public float inAirAccelerationMultiplier = 0.5f;
    public float inAirDecelerationMultiplier = 0.5f;
    private Vector2 _moveInputDirection;
    public AnimationCurve accelerationCurve = new AnimationCurve(new Keyframe(0, 0,0,1, 0,0.25f), new Keyframe(1, 1, 1, 0, 0.25f, 0));
    public AnimationCurve decelerationCurve = new AnimationCurve(new Keyframe(-1, 1,0,-1, 0,0.25f), new Keyframe(0, 0, -1, 0, 0.25f, 0));
    public AnimationCurve inverseAccelerationCurve;
    public AnimationCurve inverseDecelerationCurve;

    [Header("Jumping")]
    public float maxJumpHeight = 5;
    public float timeToJumpApex = 0.5f;
    public int maxJumps = 2;
    private int _availableJumps;
    private bool _fastFall;
    public float gravityMultiplier = 3;
    public float maxFallSpeed = -50;

    public float coyoteTime = 0.1f;
    private bool _isCoyoteTime;
    private float _timeSinceLeftGround;

    public float jumpBufferTime = 0.1f;
    private bool _waitingForJump;
    private float _timeSinceJumpPress;

    public float jumpCooldownTime = 0.2f;
    private bool _jumpInCooldown;
    private float _timeSinceLastJump;

    private bool _inAirFromJumping;
    private bool _inAirFromFalling;

    [Header("Physics")]
    public float groundCheckThickness = 0.1f;
    public LayerMask groundMask = 1;
    private Rigidbody2D _rb;
    private BoxCollider2D _boxCollider;
    private float _halfWidth;
    private float _halfHeight;
    private Vector2 _groundCheckSize;
    public float cornerCorrectionWidth = 0.1f;
    public float cornerCorrectionDistance = 1;
    private bool _isGrounded;

    [Header("Effects")]
    public GameObject airJumpParticles;
    public GameObject groundJumpParticles;
    public GameObject landParticles;

    [Header("Audio")]
    public AudioClip[] groundJumpSounds;
    public AudioClip[] airJumpSounds;
    public AudioClip[] landSounds;
    private AudioSource _audioSource;
    
    [Header("Other")]
    public Transform xpostrail;
    public Transform xvelrail;


    public event Action OnJump;
    public event Action OnGroundJump;
    public event Action OnAirJump;
    public event Action OnLand;

    private void Start()
    {
        InputManager.Get().Jump += HandleJump;
        InputManager.Get().Move += HandleMove;
        InputManager.Get().Look += HandleLook;
        
        _rb = GetComponent<Rigidbody2D>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _audioSource = GetComponent<AudioSource>();
        
        _availableJumps = maxJumps;
        
        inverseAccelerationCurve = AnimCurveUtils.InverseIncreasingCurve(accelerationCurve);
        inverseDecelerationCurve = AnimCurveUtils.InverseDecreasingCurve(decelerationCurve);

        Vector2 size = _boxCollider.size;
        _halfWidth = size.x / 2;
        _halfHeight = size.y / 2;
        _groundCheckSize = new Vector2(size.x, groundCheckThickness);

        _timeSinceJumpPress = jumpCooldownTime;
        _timeSinceLastJump = jumpCooldownTime;

        OnGroundJump += () => _audioSource.PlayOneShot(groundJumpSounds[Random.Range(0,groundJumpSounds.Length)], 0.4f);
        OnGroundJump += () => Instantiate(groundJumpParticles, transform);
        OnAirJump += () => _audioSource.PlayOneShot(airJumpSounds[Random.Range(0,airJumpSounds.Length)], 0.6f);
        OnAirJump += () => Instantiate(airJumpParticles, transform);
        OnLand += () => _audioSource.PlayOneShot(landSounds[Random.Range(0,landSounds.Length)]);
        OnLand += () => Instantiate(landParticles, transform.position - new Vector3(0, _halfHeight, 0), quaternion.identity);
    }

    private void OnDestroy()
    {
        if (GameManager.applicationIsQuitting)
            return;
        InputManager.Get().Jump -= HandleJump;
        InputManager.Get().Move -= HandleMove;
        InputManager.Get().Look -= HandleLook;
    }


    private void HandleMove(Vector2 value)
    {
        _moveInputDirection = value;
    }

    private void HandleLook(Vector2 value)
    {
        //print(value);
    }

    private bool _jumpButtonHolding;

    private void HandleJump(float value)
    {
        if (value > 0)
        {
            _jumpButtonHolding = true;
            _timeSinceJumpPress = 0;
        }
        else
        {
            _jumpButtonHolding = false;
            _fastFall = true;
        }
    }


    private void Jump()
    {
        Vector2 velocity = _rb.velocity;
        velocity.y = (2 * maxJumpHeight) / timeToJumpApex;
        _rb.velocity = velocity;

        _fastFall = !_jumpButtonHolding;
        _inAirFromJumping = true;
        _inAirFromFalling = false;
        _timeSinceLastJump = 0;
        OnJump?.Invoke();
    }

    private void GroundJump()
    {
        Jump();
        OnGroundJump?.Invoke();
    }

    private void AirJump()
    {
        _availableJumps--;
        Jump();
        OnAirJump?.Invoke();
    }

    public PlatformerController(AudioSource audioSource)
    {
        this._audioSource = audioSource;
    }

    private void FixedUpdate()
    {
        //Jumping Logic
        bool wasGrounded = _isGrounded;
        if (_rb.velocity.y > 0)
            _isGrounded = false;
        else
            _isGrounded = Physics2D.BoxCast((Vector2) transform.position - new Vector2(0, _halfHeight), _groundCheckSize, 0, Vector2.down, 0, groundMask);

        if (_isGrounded)
        {

        }

        //first frame landing on ground
        if (!wasGrounded && _isGrounded)
        {
            _fastFall = true;
            _timeSinceLeftGround = 0;
            _inAirFromJumping = false;
            _inAirFromFalling = false;
            _availableJumps = maxJumps;
            OnLand?.Invoke();
        }

        //first frame leaving ground
        if (wasGrounded && !_isGrounded)
        {
            _availableJumps--;

            if (_jumpInCooldown)
            {
                _inAirFromJumping = true;
            }
            else
                _inAirFromFalling = true;
        }

        if (_waitingForJump && !_jumpInCooldown)
        {
            if (_isGrounded || _isCoyoteTime)
                GroundJump();
            else if (_availableJumps > 0)
                AirJump();
        }

        if (_inAirFromFalling)
        {
            _timeSinceLeftGround += Time.fixedDeltaTime;
            _isCoyoteTime = _timeSinceLeftGround < coyoteTime;
        }
        else
        {
            _isCoyoteTime = false;
        }

        _timeSinceJumpPress += Time.fixedDeltaTime;
        _waitingForJump = _timeSinceJumpPress < jumpBufferTime;

        _timeSinceLastJump += Time.fixedDeltaTime;
        _jumpInCooldown = _timeSinceLastJump < jumpCooldownTime;


        Vector2 velocity = _rb.velocity;

        //Gravity
        if (velocity.y < 0)
            _fastFall = true;
        float gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2) * Time.fixedDeltaTime;
        if (_fastFall)
            gravity *= gravityMultiplier;
        velocity.y += gravity;
        velocity.y = Mathf.Max(velocity.y, maxFallSpeed);

        //Movement
        float percentSpeed = Mathf.Abs(velocity.x) / maxSpeed;
        bool a = _moveInputDirection.x < -0.01f && velocity.x <= 0.1f;
        bool b = _moveInputDirection.x > 0.01f && velocity.x >= -0.1f;
        if (a || b) //accelerate
        {
            if (_isGrounded)
                percentSpeed = Mathf.Clamp01(inverseAccelerationCurve.Evaluate(percentSpeed) + ((1 / timeToMaxSpeed) * Time.fixedDeltaTime));
            else
                percentSpeed = Mathf.Clamp01(inverseAccelerationCurve.Evaluate(percentSpeed) + ((1 / timeToMaxSpeed) * Time.fixedDeltaTime) * inAirAccelerationMultiplier);
            
            velocity.x = maxSpeed * accelerationCurve.Evaluate(percentSpeed) * Mathf.Sign(_moveInputDirection.x);
        }
        else //decelerate
        {
            if (_isGrounded)
                percentSpeed = Mathf.Clamp01(inverseDecelerationCurve.Evaluate(-percentSpeed) - ((1 / timeToStop) * Time.fixedDeltaTime));
            else
                percentSpeed = Mathf.Clamp01(inverseDecelerationCurve.Evaluate(-percentSpeed) - ((1 / timeToStop) * Time.fixedDeltaTime) * inAirDecelerationMultiplier);
            
            velocity.x = maxSpeed * decelerationCurve.Evaluate(-percentSpeed) * Mathf.Sign(velocity.x);
        }
        _rb.velocity = velocity;

        //Debug trails
        Vector2 v = xpostrail.position;
        v.x += Time.fixedDeltaTime;
        v.y = transform.position.x;
        xpostrail.position = v;
        v = xvelrail.position;
        v.x += Time.fixedDeltaTime;
        v.y = _rb.velocity.x;
        xvelrail.position = v;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + new Vector3(_halfWidth - cornerCorrectionWidth / 2, _halfHeight + cornerCorrectionDistance / 2, 0), new Vector3(cornerCorrectionWidth, cornerCorrectionDistance, 0));
        Gizmos.DrawWireCube(transform.position + new Vector3(-_halfWidth + cornerCorrectionWidth / 2, _halfHeight + cornerCorrectionDistance / 2, 0), new Vector3(cornerCorrectionWidth, cornerCorrectionDistance, 0));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position - new Vector3(0, _halfHeight, 0), _groundCheckSize);
    }
}