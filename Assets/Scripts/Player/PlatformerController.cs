using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ParticleSystemJobs;
using UnityEngine.PlayerLoop;
using Random = UnityEngine.Random;

public class PlatformerController : MonoBehaviour
{
    #region ----------------Variables----------------
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

    public float jumpCooldownTime = 0.1f; //JUMP MUST ESCAPE GROUNDED BOXCAST BEFORE COOLDOWN FINISHES
    private bool _jumpInCooldown;
    private float _timeSinceLastJump;

    private bool _inAirFromJumping;
    private bool _inAirFromFalling;
    
    private bool _jumpButtonHolding;

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
    private float _groundedDistance;

    [Header("Effects")]
    public GameObject airJumpParticles;
    public GameObject groundJumpParticles;
    public GameObject landParticles;
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;

    [Header("Audio")]
    public AudioClip[] groundJumpSounds;
    public AudioClip[] airJumpSounds;
    public AudioClip[] landSounds;
    private AudioSource _audioSource;

    [Header("Input")]
    public UnityEvent onJump;
    public UnityEvent onGroundJump;
    public UnityEvent onAirJump;
    public UnityEvent onLeaveGround;
    public UnityEvent onLand;
    private int _numberOfInterrupters;
    
    [Header("Other")]
    public Transform xpostrail;
    public Transform xvelrail;
    #endregion
    
    private void OnValidate()
    {
        GetComponents();
        SetStartingVariables();
    }

    private void Awake()
    {
        GetComponents();
        SetStartingVariables();
        AddListeners();
    }
    
    #region ----------------Awake Functions----------------
    void GetComponents()
    {
        _rb = GetComponent<Rigidbody2D>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _audioSource = GetComponent<AudioSource>();
        _animator = GetComponentInChildren<Animator>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void SetStartingVariables()
    {
        _size = _boxCollider.size;
        _halfWidth = _size.x / 2;
        _halfHeight = _size.y / 2;
        _groundCheckSize = new Vector2(_size.x, 0.001f);
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
        
        if (CheckIfGrounded(out _isGrounded, out _groundedDistance, transform.position, Vector2.zero))
        {
            _wasGrounded = true;
            _timeOnGround = Mathf.Infinity;
        }
        else
        {
            _timeSinceLeftGround = Mathf.Infinity;
        }
    }

    void AddListeners()
    {
        onLeaveGround.AddListener(() => _animator.Play("Stretch"));
        onGroundJump.AddListener(() => _audioSource.PlayOneShot(groundJumpSounds[Random.Range(0,groundJumpSounds.Length)], 0.4f));
        onGroundJump.AddListener(() => Instantiate(groundJumpParticles, transform));
        onAirJump.AddListener(() => _audioSource.PlayOneShot(airJumpSounds[Random.Range(0,airJumpSounds.Length)], 0.6f));
        onAirJump.AddListener(() => Instantiate(airJumpParticles, transform));
        onLand.AddListener(() => _audioSource.PlayOneShot(landSounds[Random.Range(0,landSounds.Length)]));
        onLand.AddListener(() => Instantiate(landParticles, transform.position - new Vector3(0, _halfHeight, 0), quaternion.identity));
        onLand.AddListener(() => _animator.Play("Squash"));
    }
    #endregion


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
    
    #region ----------------Handle Functions----------------
    private void HandleMove(Vector2 value)
    {
        _moveInputDirection = value;
    }

    private void HandleLook(Vector2 value)
    {
        //print(value);
    }


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
    #endregion

    
    private void FixedUpdate()
    {
        Vector2 velocity = _rb.velocity;
        Vector2 position = (Vector2)transform.position;
        
        JumpingUpdate(position, ref velocity);
        GravityUpdate(ref velocity);
        MovementUpdate(ref velocity);
        _rb.velocity = velocity;

        CornerCorrectionUpdate(ref position, velocity);
        SnapToGroundUpdate(ref position, velocity);
        transform.position = (Vector3)position;
        
        DebugTrailsUpdate();
    }
    
    #region ----------------FixedUpdate Functions----------------
    private RaycastHit2D CheckIfGrounded(out bool isGrounded, out float groundedDistance, Vector2 position, Vector2 velocity)
    {
        RaycastHit2D hit = Physics2D.BoxCast((Vector2)position + (Vector2.down * _halfHeight), _groundCheckSize, 0, Vector2.down, groundCheckThickness, _traversableMask);
        if (hit && (_wasGrounded || velocity.y <= 0))
        {
            isGrounded = true;
            groundedDistance = hit.distance;
        }
        else
        {
            isGrounded = false;
            groundedDistance = Mathf.Infinity;
        }
        return hit;
    }
    
    private void JumpingUpdate(Vector2 position, ref Vector2 velocity)
    {
        _wasGrounded = _isGrounded;
        CheckIfGrounded(out _isGrounded, out _groundedDistance, position, velocity);

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
            onLand?.Invoke();
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
            
            onLeaveGround?.Invoke();
        }

        if (_waitingForJump && !_jumpInCooldown)
        {
            if (_isGrounded || _isCoyoteTime)
                GroundJump(ref velocity);
            else if (_availableJumps > 0)
                AirJump(ref velocity);
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
    }

    private void GravityUpdate(ref Vector2 velocity)
    {
        //Gravity
        if (velocity.y < 0)
            _fastFall = true;
        float gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2) * Time.fixedDeltaTime;
        if (_fastFall)
            gravity *= gravityMultiplier;
        if (!_isGrounded)
            velocity.y += gravity;
        else if(!_jumpInCooldown)
            velocity.y = 0;
        velocity.y = Mathf.Max(velocity.y, maxFallSpeed);
    }

    private void MovementUpdate(ref Vector2 velocity)
    {
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
    }

    private void CornerCorrectionUpdate(ref Vector2 position, Vector2 velocity)
    {
        //Vertical Corner Correction
        if(velocity.y > 0)
        {
            Vector2 rightOrigin = (Vector2) position + new Vector2(_halfWidth, _halfHeight);
            Vector2 leftOrigin = (Vector2) position + new Vector2(-_halfWidth, _halfHeight);
            RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.up, velocity.y * Time.fixedDeltaTime * 2, _cornerCorrectionMask);
            RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.up, velocity.y * Time.fixedDeltaTime * 2, _cornerCorrectionMask);

            if (leftHit && !rightHit)
            {
                RaycastHit2D leftHitDist = Physics2D.Raycast(new Vector2(position.x, leftHit.point.y + 0.01f), Vector2.left, _halfWidth, _cornerCorrectionMask);
                if (leftHitDist && (_halfWidth - leftHitDist.distance) <= _verticalCornerCorrectionWidth)
                    position += Vector2.right * ((_halfWidth - leftHitDist.distance) + 0.05f);
            }
            else if (rightHit && !leftHit)
            {
                RaycastHit2D rightHitDist = Physics2D.Raycast(new Vector2(position.x, rightHit.point.y + 0.01f), Vector2.right, _halfWidth, _cornerCorrectionMask);
                if (rightHitDist && (_halfWidth - rightHitDist.distance) <= _verticalCornerCorrectionWidth)
                    position += Vector2.left * ((_halfWidth - rightHitDist.distance) + 0.05f);
            }
        }

        //Horizontal Corner Correction
        if (velocity.y > 0)
            return;
        if (velocity.x > 0)
        {
            Vector2 rightOrigin = (Vector2) position + new Vector2(_halfWidth, -_halfHeight);
            RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.right, velocity.x * Time.fixedDeltaTime * 2, _traversableMask);

            if (rightHit)
            {
                RaycastHit2D rightHitDist = Physics2D.Raycast(new Vector2(rightHit.point.x + 0.01f, position.y + _halfHeight), Vector2.down, _size.y, _traversableMask);
                if (rightHitDist && (_size.y - rightHitDist.distance) <= _horizontalCornerCorrectionHeight)
                    position += new Vector2(0.05f, ((_size.y - rightHitDist.distance) + 0.05f));
            }
        }
        else if (velocity.x < 0)
        {
            Vector2 leftOrigin = (Vector2) position + new Vector2(-_halfWidth, -_halfHeight);
            RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.left, -velocity.x * Time.fixedDeltaTime * 2, _traversableMask);

            if (leftHit)
            {
                RaycastHit2D leftHitDist = Physics2D.Raycast(new Vector2(leftHit.point.x - 0.05f, position.y + _halfHeight), Vector2.down, _size.y, _traversableMask);
                if (leftHitDist && (_size.y - leftHitDist.distance) <= _horizontalCornerCorrectionHeight)
                    position += new Vector2(-0.05f, ((_size.y - leftHitDist.distance) + 0.05f));
            }
        }
    }

    private void SnapToGroundUpdate(ref Vector2 position, Vector2 velocity)
    {
        //Snap To Ground
        if (!_jumpInCooldown && CheckIfGrounded(out _isGrounded,out _groundedDistance, position, velocity))
            if(_groundedDistance < Mathf.Infinity)
                position += new Vector2(0, -_groundedDistance + 0.001f);
    }

    private void DebugTrailsUpdate()
    {
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
    #endregion

    #region ----------------Jump Functions--------
    private void Jump(ref Vector2 velocity)
    {
        velocity.y = (2 * maxJumpHeight) / timeToJumpApex;

        _fastFall = !_jumpButtonHolding;
        _inAirFromJumping = true;
        _inAirFromFalling = false;
        _timeSinceLastJump = 0;
        onJump?.Invoke();
    }

    private void GroundJump(ref Vector2 velocity)
    {
        Jump(ref velocity);
        onGroundJump?.Invoke();
    }

    private void AirJump(ref Vector2 velocity)
    {
        _availableJumps--;
        Jump(ref velocity);
        onAirJump?.Invoke();
    }
    #endregion


    public void AddInterrupter() => _numberOfInterrupters++;
    public void SubtractInterrupter() => _numberOfInterrupters--;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            _rb.velocity = new Vector2(10, 20f);
        
        Gizmos.color = Color.red;
        
        var position = transform.position;
        var velocity = _rb.velocity;

        if (_rb.velocity.y > 0)
        {
            Gizmos.DrawWireCube(position + new Vector3(_halfWidth - _verticalCornerCorrectionWidth / 2, _halfHeight + velocity.y * Time.fixedDeltaTime, 0), new Vector3(_verticalCornerCorrectionWidth, velocity.y * Time.fixedDeltaTime * 2, 0));
            Gizmos.DrawWireCube(position + new Vector3(-_halfWidth + _verticalCornerCorrectionWidth / 2, _halfHeight + velocity.y * Time.fixedDeltaTime, 0), new Vector3(_verticalCornerCorrectionWidth, velocity.y * Time.fixedDeltaTime * 2, 0));
        }

        if(velocity.x > 0)
            Gizmos.DrawWireCube(position + new Vector3(_halfWidth + velocity.x * Time.fixedDeltaTime, -_size.y / 2 + _horizontalCornerCorrectionHeight / 2, 0), new Vector3(velocity.x * Time.fixedDeltaTime * 2, _horizontalCornerCorrectionHeight, 0));
        else if(velocity.x < 0)
            Gizmos.DrawWireCube(position + new Vector3(-_halfWidth + velocity.x * Time.fixedDeltaTime, -_size.y / 2 + _horizontalCornerCorrectionHeight / 2, 0), new Vector3(velocity.x * Time.fixedDeltaTime * 2, _horizontalCornerCorrectionHeight, 0));

        Gizmos.DrawWireCube(position + new Vector3(0, -_halfHeight - (groundCheckThickness / 2), 0), new Vector3(_groundCheckSize.x, groundCheckThickness, 0));
    }
}