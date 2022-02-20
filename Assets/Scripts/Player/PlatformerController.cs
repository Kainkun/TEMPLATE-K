using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class PlatformerController : MonoBehaviour
{
    #region ----------------Variables----------------

    #region ----------------Movement----------------
    [Header("Movement")]
    public float maxRunSpeed = 15;
    public float timeToMaxSpeed = 0.2f;
    public float timeToStop = 0.1f;
    public float inAirAccelerationMultiplier = 0.5f;
    public float inAirDecelerationMultiplier = 0.5f;
    private Vector2 _moveInputDirection;
    public float accelerationCurve = 3.66f;
    public float decelerationCurve = 3.66f;
    private Vector2 _velocity;
    private Vector2 _platformDelta;
    private Vector2 _lastPlatformVelocity;
    private Vector2 _position;
    //private bool _outOfControl;
    private Vector2 _outOfControlVelocity;
    
    private bool _crouching;
    public float crouchTimeToFall = 0.2f;
    public float timeToFallThroughPlatform = 0.1f;
    private float _timeCrouchingOnPlatform;
    #endregion

    #region ----------------Jumping----------------
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
    #endregion

    #region ----------------Physics----------------
    [Header("Physics")]
    public float groundCheckThickness = 0.1f;
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
    private bool _standingOnRigidBody;
    private bool _standingOnPlatform;
    private bool _standingOnMovingPlatform;
    private bool _wasStandingOnMovingPlatform;
    private RaycastHit2D _boxcastHit;
    private float _gravity;
    #endregion

    #region ----------------Effects----------------
    [Header("Effects")]
    public GameObject airJumpParticles;
    public GameObject groundJumpParticles;
    public GameObject landParticles;
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    #endregion

    #region ----------------Audio----------------
    [Header("Audio")]
    public AudioClip[] groundJumpSounds;
    public AudioClip[] airJumpSounds;
    public AudioClip[] landSounds;
    private AudioSource _audioSource;
    #endregion

    #region ----------------Input----------------
    [Header("Input")]
    public Action onJump;
    public Action onGroundJump;
    public Action onAirJump;
    public Action onLeaveGround;
    public Action onLand;
    #endregion

    #region ----------------Other----------------
    [Header("Other")]
    public Transform xpostrail;
    public Transform xveltrail;
    #endregion
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

        _gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2) * Time.fixedDeltaTime;

        _availableJumps = maxJumps;
        _timeSinceJumpPress = Mathf.Infinity;
        _timeSinceLastJump = Mathf.Infinity;
        _fastFall = true;
        _velocity = Vector2.zero;
        _rb.velocity = Vector2.zero;

        if (CheckIfGrounded())
        {
            _wasGrounded = true;
            _timeOnGround = Mathf.Infinity;
            if(_standingOnRigidBody)
            {
                _wasStandingOnMovingPlatform = true;
                MovingPlatformUpdate();
            }
        }
        else
        {
            _timeSinceLeftGround = Mathf.Infinity;
        }
    }

    void AddListeners()
    {
        onLeaveGround += () => _animator.Play("Stretch");
        onGroundJump += () => _audioSource.PlayOneShot(groundJumpSounds[Random.Range(0,groundJumpSounds.Length)], 0.4f);
        onGroundJump += () => Instantiate(groundJumpParticles, transform);
        onAirJump += () => _audioSource.PlayOneShot(airJumpSounds[Random.Range(0,airJumpSounds.Length)], 0.6f);
        onAirJump += () => Instantiate(airJumpParticles, transform);
        onLand += () => _audioSource.PlayOneShot(landSounds[Random.Range(0,landSounds.Length)]);
        onLand += () => Instantiate(landParticles, transform.position - new Vector3(0, _halfHeight, 0), quaternion.identity);
        onLand += () => _animator.Play("Squash");
    }
    #endregion


    private void Start()
    {
        InputManager.Get().Jump += HandleJump;
        InputManager.Get().Crouch += HandleCrouch;
        InputManager.Get().Move += HandleMove;
        InputManager.Get().Look += HandleLook;
    }

    private void OnDestroy()
    {
        if (GameManager.applicationIsQuitting)
            return;
        InputManager.Get().Jump -= HandleJump;
        InputManager.Get().Jump -= HandleCrouch;
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

    void HandleCrouch(float value)
    {
        _crouching = value > 0;
    }
    #endregion

    
    private void FixedUpdate()
    {
        _velocity = _rb.velocity;
        _position = (Vector2)transform.position;

        CheckIfGrounded();
        CheckPlatformFall();
        
        MovingPlatformUpdate();
        JumpingUpdate();
        GravityUpdate();
        MovementUpdate();
        
        SnapToGroundUpdate();
        CornerCorrectionUpdate();
        
        _rb.velocity = _velocity;
        transform.position = _position;
        
        DebugTrailsUpdate();
    }
    
    #region ----------------FixedUpdate Functions----------------
    private bool CheckIfGrounded()
    {
        _wasGrounded = _isGrounded;

        _standingOnPlatform = false;
        
        _boxcastHit = Physics2D.BoxCast((Vector2)_position + (Vector2.down * _halfHeight), _groundCheckSize, 0, Vector2.down, Mathf.Max(groundCheckThickness, -_velocity.y * Time.fixedDeltaTime), GameData.defaultGroundMask);
        if(!_boxcastHit)
        {
            _boxcastHit = Physics2D.BoxCast((Vector2) _position + (Vector2.down * _halfHeight), _groundCheckSize, 0, Vector2.down, Mathf.Max(groundCheckThickness, -_velocity.y * Time.fixedDeltaTime), GameData.platformMask);
            if(_boxcastHit)
                _standingOnPlatform = true;
        }

        if ((_boxcastHit && !_standingOnPlatform) || (_standingOnPlatform && _velocity.y <= 0))
        {
            _isGrounded = true;
            _groundedDistance = _boxcastHit.distance;
            _timeOnGround += Time.fixedDeltaTime;
            if (_boxcastHit.rigidbody)
                _standingOnRigidBody = true;
            else
                _standingOnRigidBody = false;
            return true;
        }
        else
        {
            _isGrounded = false;
            _groundedDistance = Mathf.Infinity;
            _standingOnRigidBody = false;
            return false;
        }
    }

    private void CheckPlatformFall()
    {
        if (_crouching)
            _timeCrouchingOnPlatform = Mathf.Max(_timeCrouchingOnPlatform + Time.deltaTime, 0);
        else
            _timeCrouchingOnPlatform = Mathf.Min(_timeCrouchingOnPlatform - Time.deltaTime, 0);
        
        if (_timeCrouchingOnPlatform >= crouchTimeToFall)
        {
            // _isGrounded = false;
            // _groundedDistance = Mathf.Infinity;
            // _timeOnGround += Time.fixedDeltaTime;
            // _standingOnRigidBody = false;
            gameObject.layer = LayerMask.NameToLayer("PlayerPlatformFall");
        }
        else if(_timeCrouchingOnPlatform < -timeToFallThroughPlatform)
        {
            gameObject.layer = LayerMask.NameToLayer("Player");
        }

    }
    
    private void JumpingUpdate()
    {
        //first frame landing on ground
        if (!_wasGrounded && _isGrounded)
        {
            _fastFall = true;
            _timeSinceLeftGround = 0;
            _inAirFromJumping = false;
            _inAirFromFalling = false;
            _availableJumps = maxJumps;
            _outOfControlVelocity = Vector2.zero;
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
    }

    private void GravityUpdate()
    {
        //Gravity
        if (_velocity.y < 0)
            _fastFall = true;
        float currentGravity = _gravity;
        if (_fastFall)
            currentGravity *= gravityMultiplier;
        
        if (!_isGrounded || (_standingOnPlatform && _crouching))
            _velocity.y += currentGravity;
        else if(!_jumpInCooldown && !_standingOnRigidBody && !(_jumpButtonHolding && _velocity.y > 0))
            _velocity.y = 0;
        _velocity.y = Mathf.Max(_velocity.y, maxFallSpeed);
    }

    private void MovementUpdate()
    {
        _velocity -= _outOfControlVelocity;
        
        //Movement
        float percentSpeed = Mathf.Abs(_velocity.x) / maxRunSpeed;
        bool a = _moveInputDirection.x < -0.01f && _velocity.x <= 0.1f;
        bool b = _moveInputDirection.x > 0.01f && _velocity.x >= -0.1f;
        if ((a || b) && percentSpeed <= 1) //accelerate with the flow
        {
            if (_isGrounded)
                percentSpeed = Mathf.Clamp01(Mathf.Pow(percentSpeed, accelerationCurve) + ((1 / timeToMaxSpeed) * Time.fixedDeltaTime));
            else
                percentSpeed = Mathf.Clamp01(Mathf.Pow(percentSpeed, accelerationCurve) + ((1 / timeToMaxSpeed) * Time.fixedDeltaTime) * inAirAccelerationMultiplier);
            _velocity.x = maxRunSpeed * Mathf.Pow(percentSpeed * Mathf.Abs(_moveInputDirection.x), 1/accelerationCurve) * Mathf.Sign(_moveInputDirection.x);
        }
        else //decelerate against the flow
        {
            if (_isGrounded)
                percentSpeed = Mathf.Max(Mathf.Pow(percentSpeed, decelerationCurve) - ((1 / timeToStop) * Time.fixedDeltaTime), 0);
            else
                percentSpeed = Mathf.Max(Mathf.Pow(percentSpeed, decelerationCurve) - ((1 / timeToStop) * Time.fixedDeltaTime) * inAirDecelerationMultiplier, 0);
            
            _velocity.x = maxRunSpeed * Mathf.Pow(percentSpeed, 1/decelerationCurve) * Mathf.Sign(_velocity.x);
        }

        _velocity += _outOfControlVelocity;
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void MovingPlatformUpdate()
    {
        _wasStandingOnMovingPlatform = _standingOnMovingPlatform;
        _standingOnMovingPlatform = false;
        MovingKinematic movingKinematic = null;
        
        if(_standingOnRigidBody)
        {
            movingKinematic = _boxcastHit.rigidbody.GetComponent<MovingKinematic>();
            if (movingKinematic)
                _standingOnMovingPlatform = true;
        }
        
        if(_standingOnMovingPlatform && movingKinematic)
        {
            _platformDelta = movingKinematic.Delta;
            _lastPlatformVelocity = movingKinematic.Velocity;
            
            _position.x += _platformDelta.x;
            if(_lastPlatformVelocity.y > 1)
                _velocity = _lastPlatformVelocity;
        }

        if (_wasStandingOnMovingPlatform && !_standingOnMovingPlatform) //if leaving moving platform
        {
            _lastPlatformVelocity.y = Mathf.Max(_lastPlatformVelocity.y, 0);
            _outOfControlVelocity = _lastPlatformVelocity;
            //_velocity += _lastPlatformVelocity;
            _lastPlatformVelocity = Vector2.zero;
            _platformDelta = Vector2.zero;
        }
    }

    private void CornerCorrectionUpdate()
    {
        //Vertical Corner Correction
        if(_velocity.y > 0)
        {
            Vector2 rightOrigin = (Vector2) _position + new Vector2(_halfWidth, _halfHeight);
            Vector2 leftOrigin = (Vector2) _position + new Vector2(-_halfWidth, _halfHeight);
            RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.up, _velocity.y * Time.fixedDeltaTime * 2, GameData.defaultGroundMask);
            RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.up, _velocity.y * Time.fixedDeltaTime * 2, GameData.defaultGroundMask);

            if (leftHit && !rightHit)
            {
                RaycastHit2D leftHitDist = Physics2D.Raycast(new Vector2(_position.x, leftHit.point.y + 0.01f), Vector2.left, _halfWidth, GameData.defaultGroundMask);
                if (leftHitDist && (_halfWidth - leftHitDist.distance) <= _verticalCornerCorrectionWidth)
                    _position += Vector2.right * ((_halfWidth - leftHitDist.distance) + 0.05f);
            }
            else if (rightHit && !leftHit)
            {
                RaycastHit2D rightHitDist = Physics2D.Raycast(new Vector2(_position.x, rightHit.point.y + 0.01f), Vector2.right, _halfWidth, GameData.defaultGroundMask);
                if (rightHitDist && (_halfWidth - rightHitDist.distance) <= _verticalCornerCorrectionWidth)
                    _position += Vector2.left * ((_halfWidth - rightHitDist.distance) + 0.05f);
            }
        }

        //Horizontal Corner Correction
        if (_velocity.y > 0)
            return;
        if (_velocity.x > 0)
        {
            Vector2 rightOrigin = (Vector2) _position + new Vector2(_halfWidth, -_halfHeight);
            RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.right, _velocity.x * Time.fixedDeltaTime * 2, GameData.traversableMask);

            if (rightHit)
            {
                RaycastHit2D rightHitDist = Physics2D.Raycast(new Vector2(rightHit.point.x + 0.01f, _position.y + _halfHeight), Vector2.down, _size.y, GameData.traversableMask);
                if (rightHitDist && (_size.y - rightHitDist.distance) <= _horizontalCornerCorrectionHeight)
                    _position += new Vector2(0.05f, ((_size.y - rightHitDist.distance) + 0.05f));
            }
        }
        else if (_velocity.x < 0)
        {
            Vector2 leftOrigin = (Vector2) _position + new Vector2(-_halfWidth, -_halfHeight);
            RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.left, -_velocity.x * Time.fixedDeltaTime * 2, GameData.traversableMask);

            if (leftHit)
            {
                RaycastHit2D leftHitDist = Physics2D.Raycast(new Vector2(leftHit.point.x - 0.05f, _position.y + _halfHeight), Vector2.down, _size.y, GameData.traversableMask);
                if (leftHitDist && (_size.y - leftHitDist.distance) <= _horizontalCornerCorrectionHeight)
                    _position += new Vector2(-0.05f, ((_size.y - leftHitDist.distance) + 0.05f));
            }
        }
    }

    private void SnapToGroundUpdate()
    {
        //Snap To Ground
        if (!_jumpInCooldown && _isGrounded && _velocity.y <= 0 && !(_jumpButtonHolding && _velocity.y > 0))
            if(_groundedDistance < Mathf.Infinity)
                _position += new Vector2(0, -_groundedDistance + 0.001f);
    }

    private void DebugTrailsUpdate()
    {
        if (!(xpostrail && xveltrail))
            return;
            
        //Debug trails
        Vector2 v = xpostrail.position;
        v.x += Time.fixedDeltaTime;
        v.y = transform.position.x;
        xpostrail.position = v;
        v = xveltrail.position;
        v.x += Time.fixedDeltaTime;
        v.y = _rb.velocity.x;
        xveltrail.position = v;
    }
    #endregion

    #region ----------------Jump Functions--------
    private void Jump()
    {
        _velocity.y = (2 * maxJumpHeight) / timeToJumpApex;

        _fastFall = !_jumpButtonHolding;
        _inAirFromJumping = true;
        _inAirFromFalling = false;
        _timeSinceLastJump = 0;
        onJump?.Invoke();
    }

    private void GroundJump()
    {
        Jump();
        onGroundJump?.Invoke();
    }

    private void AirJump()
    {
        _availableJumps--;
        _outOfControlVelocity = Vector2.zero;
        
        // bool a = _moveInputDirection.x < -0.01f && _velocity.x >= 0.1f;
        // bool b = _moveInputDirection.x > 0.01f && _velocity.x <= -0.1f;
        // if (a || b) //trying to jump in opposite direction
        //     _velocity.x = 0;
        
        Jump();
        onAirJump?.Invoke();
    }
    #endregion
    
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

        Gizmos.DrawWireCube(position + new Vector3(0, -_halfHeight - (Mathf.Max(groundCheckThickness, -_velocity.y * Time.fixedDeltaTime) / 2), 0), new Vector3(_groundCheckSize.x, Mathf.Max(groundCheckThickness, -_velocity.y * Time.fixedDeltaTime), 0));
    }
}