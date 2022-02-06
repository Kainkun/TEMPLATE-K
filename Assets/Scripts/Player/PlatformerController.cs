using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class PlatformerController : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 15;
    public float timeToMaxSpeed = 0.2f;
    public float timeToStop = 0.1f;
    public float inAirAccelerationMultiplier = 0.5f;
    public float inAirDecelerationMultiplier = 0.5f;
    private Vector2 _moveInputDirection;
    public AnimationCurve accelerationCurve = new AnimationCurve(new Keyframe(0, 0,0,1, 0,0.25f), new Keyframe(1, 1, 1, 0, 0.25f, 0));
    public AnimationCurve decelerationCurve = new AnimationCurve(new Keyframe(-1, 1,0,-1, 0,0.25f), new Keyframe(0, 0, -1, 0, 0.25f, 0));
    public AnimationCurve inverseAccelerationCurve;
    public AnimationCurve inverseDecelerationCurve;
    private Vector2 _velocity;

    [Header("Jumping")]
    public float maxJumpHeight = 5;
    public float timeToJumpApex = 0.4f;
    public int maxJumps = 2;
    private int _availableJumps;
    private bool _fastFall;
    public float gravityMultiplier = 2;
    public float maxFallSpeed = -50;

    public float coyoteTime = 0.1f;
    private bool _isCoyoteTime;
    private float _timeSinceLeftGround;

    public float jumpBufferTime = 0.1f;
    private bool _waitingForJump;
    private float _timeSinceJumpPress;

    public float jumpCooldownTime = 0.1f;
    private bool _jumpInCooldown;
    private float _timeSinceLastJump;

    private bool _inAirFromJumping;
    private bool _inAirFromFalling;

    [Header("Physics")]
    public float groundCheckThickness = 0.1f;
    private LayerMask _traversableMask;
    private LayerMask _cornerCorrectionMask;
    private Rigidbody2D _rb;
    private BoxCollider2D _boxCollider;
    private Vector2 _size;
    private float _halfWidth;
    private float _halfHeight;
    private Vector2 _groundCheckSize;
    private float _timeOnGround;
    [Range(0,0.99f)]
    public float verticalCornerCorrectionWidthPercent = 0.5f;
    private float _verticalCornerCorrectionWidth;
    [Range(0,0.99f)]
    public float horizontalCornerCorrectionHeightPercent = 0.2f;
    private float _horizontalCornerCorrectionHeight;
    private bool _isGrounded;
    private bool _wasGrounded;

    [Header("Effects")]
    public GameObject airJumpParticles;
    public GameObject groundJumpParticles;
    public GameObject landParticles;
    private SpriteRenderer _spriteRenderer;
    private Transform _spriteRendererPivot;
    private Animator _animator;

    [Header("Audio")]
    public AudioClip[] groundJumpSounds;
    public AudioClip[] airJumpSounds;
    public AudioClip[] landSounds;
    private AudioSource _audioSource;
    
    [Header("Other")]
    public Transform xpostrail;
    public Transform xvelrail;


    public UnityEvent OnJump;
    public UnityEvent OnGroundJump;
    public UnityEvent OnAirJump;
    public UnityEvent OnLeaveGround;
    public UnityEvent OnLand;

    void GetComponents()
    {
        _rb = GetComponent<Rigidbody2D>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _audioSource = GetComponent<AudioSource>();
        _animator = GetComponentInChildren<Animator>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _spriteRendererPivot = _spriteRenderer.transform.parent;
    }

    void SetStartingVariables()
    {
        _size = _boxCollider.size;
        _halfWidth = _size.x / 2;
        _halfHeight = _size.y / 2;
        _groundCheckSize = new Vector2(_size.x, groundCheckThickness);
        _verticalCornerCorrectionWidth = _halfWidth * verticalCornerCorrectionWidthPercent;
        _horizontalCornerCorrectionHeight = _size.y * horizontalCornerCorrectionHeightPercent;
        
        inverseAccelerationCurve = AnimCurveUtils.InverseIncreasingCurve(accelerationCurve);
        inverseDecelerationCurve = AnimCurveUtils.InverseDecreasingCurve(decelerationCurve);
        
        _traversableMask = LayerMask.GetMask("Default", "Platform");
        _cornerCorrectionMask = LayerMask.GetMask("Default");

        _availableJumps = maxJumps;
        _timeSinceJumpPress = Mathf.Infinity;
        _timeSinceLastJump = Mathf.Infinity;
        _fastFall = true;
        
        if (CheckIfGrounded())
        {
            _isGrounded = true;
            _wasGrounded = true;
            _timeOnGround = Mathf.Infinity;
        }
        else
        {
            _timeSinceLeftGround = Mathf.Infinity;
        }
    }

    private void OnValidate()
    {
        GetComponents();
        SetStartingVariables();
    }

    private void Awake()
    {
        GetComponents();
        SetStartingVariables();

        OnLeaveGround.AddListener(() => _animator.Play("Stretch"));
        OnGroundJump.AddListener(() => _audioSource.PlayOneShot(groundJumpSounds[Random.Range(0,groundJumpSounds.Length)], 0.4f));
        OnGroundJump.AddListener(() => Instantiate(groundJumpParticles, transform));
        OnAirJump.AddListener(() => _audioSource.PlayOneShot(airJumpSounds[Random.Range(0,airJumpSounds.Length)], 0.6f));
        OnAirJump.AddListener(() => Instantiate(airJumpParticles, transform));
        OnLand.AddListener(() => _audioSource.PlayOneShot(landSounds[Random.Range(0,landSounds.Length)]));
        OnLand.AddListener(() => Instantiate(landParticles, transform.position - new Vector3(0, _halfHeight, 0), quaternion.identity));
        OnLand.AddListener(() => _animator.Play("Squash"));
    }

    private void Start()
    {
        InputManager.Get().Jump += HandleJump;
        InputManager.Get().Move += HandleMove;
        InputManager.Get().Look += HandleLook;
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

    
    private bool CheckIfGrounded() => Physics2D.BoxCast((Vector2) transform.position - new Vector2(0, _halfHeight), _groundCheckSize, 0, Vector2.down, 0, _traversableMask);

    private void FixedUpdate()
    {
        //Jumping Logic
        _wasGrounded = _isGrounded;
        if (_rb.velocity.y > 0)
            _isGrounded = false;
        else
            _isGrounded = CheckIfGrounded();

        if (_isGrounded)
        {
            _timeOnGround += Time.fixedDeltaTime;
        }

        //first frame landing on ground
        if (!_wasGrounded && _isGrounded)
        {
            _fastFall = true;
            _timeSinceLeftGround = 0;
            _inAirFromJumping = false;
            _inAirFromFalling = false;
            _availableJumps = maxJumps;
            OnLand?.Invoke();
        }

        //first frame leaving ground
        if (_wasGrounded && !_isGrounded)
        {
            _timeOnGround = 0;
            _availableJumps--;

            if (_jumpInCooldown)
                _inAirFromJumping = true;
            else
                _inAirFromFalling = true;
            
            OnLeaveGround.Invoke();
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


        _velocity = _rb.velocity;

        //Gravity
        if (_velocity.y < 0)
            _fastFall = true;
        float gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2) * Time.fixedDeltaTime;
        if (_fastFall)
            gravity *= gravityMultiplier;
        _velocity.y += gravity;
        _velocity.y = Mathf.Max(_velocity.y, maxFallSpeed);

        //Movement
        float percentSpeed = Mathf.Abs(_velocity.x) / maxSpeed;
        bool a = _moveInputDirection.x < -0.01f && _velocity.x <= 0.1f;
        bool b = _moveInputDirection.x > 0.01f && _velocity.x >= -0.1f;
        if (a || b) //accelerate
        {
            if (_isGrounded)
                percentSpeed = Mathf.Clamp01(inverseAccelerationCurve.Evaluate(percentSpeed) + ((1 / timeToMaxSpeed) * Time.fixedDeltaTime));
            else
                percentSpeed = Mathf.Clamp01(inverseAccelerationCurve.Evaluate(percentSpeed) + ((1 / timeToMaxSpeed) * Time.fixedDeltaTime) * inAirAccelerationMultiplier);
            
            _velocity.x = maxSpeed * accelerationCurve.Evaluate(percentSpeed) * Mathf.Sign(_moveInputDirection.x);
        }
        else //decelerate
        {
            if (_isGrounded)
                percentSpeed = Mathf.Clamp01(inverseDecelerationCurve.Evaluate(-percentSpeed) - ((1 / timeToStop) * Time.fixedDeltaTime));
            else
                percentSpeed = Mathf.Clamp01(inverseDecelerationCurve.Evaluate(-percentSpeed) - ((1 / timeToStop) * Time.fixedDeltaTime) * inAirDecelerationMultiplier);
            
            _velocity.x = maxSpeed * decelerationCurve.Evaluate(-percentSpeed) * Mathf.Sign(_velocity.x);
        }
        _rb.velocity = _velocity;
        
        //Vertical Corner Correction
        if(_velocity.y > 0)
        {
            Vector2 rightOrigin = (Vector2) transform.position + new Vector2(_halfWidth, _halfHeight);
            Vector2 leftOrigin = (Vector2) transform.position + new Vector2(-_halfWidth, _halfHeight);
            RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.up, _velocity.y * Time.fixedDeltaTime * 2, _cornerCorrectionMask);
            RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.up, _velocity.y * Time.fixedDeltaTime * 2, _cornerCorrectionMask);

            if (leftHit && !rightHit)
            {
                RaycastHit2D leftHitDist = Physics2D.Raycast(new Vector2(transform.position.x, leftHit.point.y + 0.01f), Vector2.left, _halfWidth, _cornerCorrectionMask);
                if (leftHitDist && (_halfWidth - leftHitDist.distance) <= _verticalCornerCorrectionWidth)
                    transform.position += Vector3.right * ((_halfWidth - leftHitDist.distance) + 0.01f);
            }
            else if (rightHit && !leftHit)
            {
                RaycastHit2D rightHitDist = Physics2D.Raycast(new Vector2(transform.position.x, rightHit.point.y + 0.01f), Vector2.right, _halfWidth, _cornerCorrectionMask);
                if (rightHitDist && (_halfWidth - rightHitDist.distance) <= _verticalCornerCorrectionWidth)
                    transform.position += Vector3.left * ((_halfWidth - rightHitDist.distance) + 0.01f);
            }
        }

        //Horizontal Corner Correction
        if (_velocity.x > 0)
        {
            Vector2 rightOrigin = (Vector2) transform.position + new Vector2(_halfWidth, -_halfHeight);
            RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.right, _velocity.x * Time.fixedDeltaTime * 2, _traversableMask);

            if (rightHit)
            {
                print(rightHit);
                RaycastHit2D rightHitDist = Physics2D.Raycast(new Vector2(rightHit.point.x + 0.01f, transform.position.y + _halfHeight), Vector2.down, _size.y, _traversableMask);
                if (rightHitDist && (_size.y - rightHitDist.distance) <= _horizontalCornerCorrectionHeight)
                    transform.position += new Vector3(0.01f, ((_size.y - rightHitDist.distance) + 0.01f), 0);
            }
        }
        else if (_velocity.x < 0)
        {
            Vector2 leftOrigin = (Vector2) transform.position + new Vector2(-_halfWidth, -_halfHeight);
            RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.left, -_velocity.x * Time.fixedDeltaTime * 2, _traversableMask);

            if (leftHit)
            {
                RaycastHit2D leftHitDist = Physics2D.Raycast(new Vector2(leftHit.point.x - 0.01f, transform.position.y + _halfHeight), Vector2.down, _size.y, _traversableMask);
                if (leftHitDist && (_size.y - leftHitDist.distance) <= _horizontalCornerCorrectionHeight)
                    transform.position += new Vector3(-0.01f, ((_size.y - leftHitDist.distance) + 0.01f), 0);
            }
        }

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
        if (!Application.isPlaying)
            _velocity = new Vector2(10, 20f);
        
        Gizmos.color = Color.red;

        if (_velocity.y > 0)
        {
            Gizmos.DrawWireCube(transform.position + new Vector3(_halfWidth - _verticalCornerCorrectionWidth / 2, _halfHeight + _velocity.y * Time.fixedDeltaTime, 0), new Vector3(_verticalCornerCorrectionWidth, _velocity.y * Time.fixedDeltaTime * 2, 0));
            Gizmos.DrawWireCube(transform.position + new Vector3(-_halfWidth + _verticalCornerCorrectionWidth / 2, _halfHeight + _velocity.y * Time.fixedDeltaTime, 0), new Vector3(_verticalCornerCorrectionWidth, _velocity.y * Time.fixedDeltaTime * 2, 0));
        }

        if(_velocity.x > 0)
            Gizmos.DrawWireCube(transform.position + new Vector3(_halfWidth + _velocity.x * Time.fixedDeltaTime, -_size.y / 2 + _horizontalCornerCorrectionHeight / 2, 0), new Vector3(_velocity.x * Time.fixedDeltaTime * 2, _horizontalCornerCorrectionHeight, 0));
        else if(_velocity.x < 0)
            Gizmos.DrawWireCube(transform.position + new Vector3(-_halfWidth + _velocity.x * Time.fixedDeltaTime, -_size.y / 2 + _horizontalCornerCorrectionHeight / 2, 0), new Vector3(_velocity.x * Time.fixedDeltaTime * 2, _horizontalCornerCorrectionHeight, 0));
        
        Gizmos.DrawWireCube(transform.position - new Vector3(0, _halfHeight, 0), _groundCheckSize);
    }
}