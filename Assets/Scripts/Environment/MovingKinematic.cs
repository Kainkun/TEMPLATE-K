using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;

public class MovingKinematic : MonoBehaviour
{
    private Vector2 _startPosition;
    private Rigidbody2D rb;
    private Vector2 _currentPosition;
    private Vector2 _nextFramePosition;
    private Vector2 _velocity;
    private Vector2 _delta;

    public Vector2 StartPosition => _startPosition;
    public Vector2 CurrentPosition => _nextFramePosition;
    public Vector2 Velocity => _velocity;
    public Vector2 Delta => _delta;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        _startPosition = transform.position;
        _currentPosition = _startPosition;
        _nextFramePosition = _startPosition;
    }

    public void MovementUpdate(Vector2 position)
    {
        rb.MovePosition(_nextFramePosition);
        _currentPosition = _nextFramePosition;

        _nextFramePosition = position;
        
        _delta = _nextFramePosition - _currentPosition;
        _velocity = _delta / Time.fixedDeltaTime;
    }
}
