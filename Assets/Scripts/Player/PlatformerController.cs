using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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
    private Vector2 _kinematicDelta;
    private Vector2 _lastKinematicVelocity;
    private Vector2 _position;
    //private bool _outOfControl;
    private Vector2 _outOfControlVelocity;
    
    private bool _crouching;
    private bool _crouchHeld;
    public float crouchTimeToFall = 0.2f;
    public float timeToFallThroughPlatform = 0.1f;
    private float _timeCrouching;
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
    private bool _jumpHolding;
    #endregion

    #region ----------------Physics----------------
    [Header("Physics")]
    public float groundCheckThickness = 0.1f;
    private Rigidbody2D _rb;
    private BoxCollider2D _boxCollider;
    private Vector2 _size;
    private float _halfWidth;
    private float _halfHeight;
    private Vector2 _groundCeilingCheckSize;
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
    private bool _standingOnMovingKinematic;
    private bool _wasStandingOnMovingKinematic;
    private RaycastHit2D _groundHit;
    private float _gravity;
    public float minimumStickFallVelocity = -15;
    private bool _isCrushed;
    public Action onCrushed;
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
        _groundCeilingCheckSize = new Vector2(_size.x, 0.001f);
        _verticalCornerCorrectionWidth = _halfWidth * verticalCornerCorrectionWidthPercent;
        _horizontalCornerCorrectionHeight = _size.y * horizontalCornerCorrectionHeightPercent;

        _gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2) * Time.fixedDeltaTime;

        _availableJumps = maxJumps;
        _timeSinceJumpPress = Mathf.Infinity;
        _timeSinceLastJump = Mathf.Infinity;
        //_fastFall = true;
        _position = transform.position;
        _velocity = Vector2.zero;
        _rb.velocity = Vector2.zero;

        if (CheckIfGrounded())
        {
            _wasGrounded = true;
            _timeOnGround = Mathf.Infinity;
            if(_standingOnRigidBody)
            {
                _wasStandingOnMovingKinematic = true;
                UpdateMovingKinematic();
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
            //_fastFall = true;
        }
    }

    void HandleCrouch(float value)
    {
        _crouching = value > 0;
    }
    #endregion
    
    
    #region ----------------FixedUpdate Functions----------------
    private void FixedUpdate()
    {
        _position = (Vector2)transform.position;

        CheckIfGrounded();
        CheckPlatformFall();
        
        UpdateGravity();
        UpdateJumping();
        UpdateMovingKinematic();
        UpdateMovement();
        UpdateCollision();
        
        UpdateSnapToGround();
        UpdateCornerCorrection();
        
        _rb.velocity = _velocity;
        transform.position = _position;
        
        UpdateDebugTrails();
    }
    
    private bool CheckIfGrounded()
    {
        _wasGrounded = _isGrounded;

        _standingOnPlatform = false;
        
        RaycastHit2D _DefaultGroundHit = Physics2D.BoxCast((Vector2)_position + (Vector2.down * _halfHeight), _groundCeilingCheckSize, 0, Vector2.down, Mathf.Max(groundCheckThickness, -_velocity.y * Time.fixedDeltaTime), GameData.defaultGroundMask);
        RaycastHit2D _PlatformHit = Physics2D.BoxCast((Vector2) _position + (Vector2.down * _halfHeight), _groundCeilingCheckSize, 0, Vector2.down, Mathf.Max(groundCheckThickness, -_velocity.y * Time.fixedDeltaTime), GameData.platformMask);

        if(_PlatformHit && _velocity.y <= 0 && !_crouchHeld)
        {
            _groundHit = _PlatformHit;
            _standingOnPlatform = true;
        }
        else
        {
            _groundHit = _DefaultGroundHit;
        }

        if (_groundHit)
        {
            _isGrounded = true;
            _groundedDistance = _groundHit.distance;
            _timeOnGround += Time.fixedDeltaTime;
            if (_groundHit.rigidbody)
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
            _timeCrouching = Mathf.Max(_timeCrouching + Time.deltaTime, 0);
        else
            _timeCrouching = Mathf.Min(_timeCrouching - Time.deltaTime, 0);
        
        if (_timeCrouching >= crouchTimeToFall)
        {
            _crouchHeld = true;
            gameObject.layer = LayerMask.NameToLayer("PlayerPlatformFall");
        }
        else if(_timeCrouching < -timeToFallThroughPlatform)
        {
            _crouchHeld = false;
            gameObject.layer = LayerMask.NameToLayer("Player");
        }

    }
    
    private void UpdateJumping()
    {
        //first frame landing on ground
        if (!_wasGrounded && _isGrounded)
        {
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

    private void UpdateGravity()
    {
        _jumpHolding = _jumpButtonHolding && _inAirFromJumping && _velocity.y > 0;
        
        //Gravity
        if ((!_jumpButtonHolding || _velocity.y < 0) && !_isCoyoteTime && !_isGrounded)
            _fastFall = true;
        else
            _fastFall = false;
        float currentGravity = _gravity;
        if (_fastFall)
            currentGravity *= gravityMultiplier;
        
        if(_isGrounded && !_jumpInCooldown)
            _velocity.y = 0;
        else
            _velocity.y += currentGravity;

        _velocity.y = Mathf.Max(_velocity.y, maxFallSpeed);
    }

    private void UpdateMovement()
    {
        _velocity -= _outOfControlVelocity;
        
        //Movement
        float percentSpeed = Mathf.Abs(_velocity.x) / maxRunSpeed;
        bool movingWithVelocity = 
            _moveInputDirection.x < -0.01f && _velocity.x <= 0.1f ||
            _moveInputDirection.x > 0.01f && _velocity.x >= -0.1f;
        if (movingWithVelocity && percentSpeed <= 1) //accelerate with the flow
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
    private void UpdateMovingKinematic()
    {
        _wasStandingOnMovingKinematic = _standingOnMovingKinematic;
        _standingOnMovingKinematic = false;
        MovingKinematic movingKinematic = null;
        
        if(_standingOnRigidBody)
        {
            movingKinematic = _groundHit.rigidbody.GetComponent<MovingKinematic>();
            if (movingKinematic)
                _standingOnMovingKinematic = true;
        }

        bool leavingMovingKinematic = false;
        
        if(movingKinematic)
        {
            _kinematicDelta = movingKinematic.PreviousFrameDelta;
            _lastKinematicVelocity = movingKinematic.PreviousFrameVelocity;

            float jerk = movingKinematic.NextFrameDelta.y - movingKinematic.PreviousFrameDelta.y;
            //if kinematic falls too fast, release player
            leavingMovingKinematic |= movingKinematic.NextFrameVelocity.y < minimumStickFallVelocity;
            //if kinematic decelerates too fast, release player
            leavingMovingKinematic |= jerk < -0.1f;
            
            if(!leavingMovingKinematic)
                _position += _kinematicDelta;

            // if(movingKinematic.Velocity.y > 1)
            //     movingKinematic.Velocity;
        }

        leavingMovingKinematic |= _wasStandingOnMovingKinematic && !_standingOnMovingKinematic;

        if (leavingMovingKinematic) //if leaving moving kinematic
        {
            _position.y += groundCheckThickness + 0.01f;

            _outOfControlVelocity = _lastKinematicVelocity;
            _outOfControlVelocity.y = Mathf.Max(_outOfControlVelocity.y, maxFallSpeed);
            _velocity += _outOfControlVelocity;
            _lastKinematicVelocity = Vector2.zero;
            _kinematicDelta = Vector2.zero;
        }
    }

    void UpdateCollision()
    {
        RaycastHit2D headBump = Physics2D.BoxCast((Vector2) _position + (Vector2.up * _halfHeight), _groundCeilingCheckSize, 0, Vector2.up, 0.1f, GameData.defaultGroundMask);

        if(headBump)
            _velocity.y = Mathf.Min(_velocity.y, 0);

        bool headBumpIsMovingKinematic = false;
        if (headBump)
            headBumpIsMovingKinematic = headBump.transform.GetComponent<MovingKinematic>();

        bool crushed = (headBump && _standingOnMovingKinematic) || (headBumpIsMovingKinematic && _isGrounded);
        
        if (crushed && !_isCrushed)
            onCrushed?.Invoke();

        _isCrushed = crushed;
    }

    private void UpdateCornerCorrection()
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

    private void UpdateSnapToGround()
    {
        //Snap To Ground
        if (!_jumpInCooldown && !_jumpHolding && _isGrounded)
            if(_groundedDistance < Mathf.Infinity)
                _position += new Vector2(0, -_groundedDistance + 0.001f);
    }

    private void UpdateDebugTrails()
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
        _velocity.y += (2 * maxJumpHeight) / timeToJumpApex;

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
        
        bool movingAgainstVelocity =
            _moveInputDirection.x < -0.01f && _velocity.x >= 0.1f ||
            _moveInputDirection.x > 0.01f && _velocity.x <= -0.1f;
        if (movingAgainstVelocity) //trying to jump in opposite direction
            _velocity.x = 0;

        _velocity.y = 0;
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

        Gizmos.DrawWireCube(position + new Vector3(0, -_halfHeight - (Mathf.Max(groundCheckThickness, -_velocity.y * Time.fixedDeltaTime) / 2), 0), new Vector3(_groundCeilingCheckSize.x, Mathf.Max(groundCheckThickness, -_velocity.y * Time.fixedDeltaTime), 0));
    }
}