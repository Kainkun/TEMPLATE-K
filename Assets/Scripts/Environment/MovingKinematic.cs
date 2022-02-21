using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingKinematic : MonoBehaviour
{
    private Vector2 _startPosition;
    private Rigidbody2D rb;
    private Vector2 _currentPosition;
    private Vector2 _nextFramePosition;
    private Vector2 _nextFrameVelocity;
    private Vector2 _previousFrameVelocity;
    private Vector2 _nextFrameDelta;
    private Vector2 _previousFrameDelta;

    public Vector2 StartPosition => _startPosition;
    public Vector2 CurrentPosition => _nextFramePosition;
    public Vector2 NextFrameVelocity => _nextFrameVelocity;
    public Vector2 PreviousFrameVelocity => _previousFrameVelocity;
    public Vector2 NextFrameDelta => _nextFrameDelta;
    public Vector2 PreviousFrameDelta => _previousFrameDelta;

    private void OnValidate()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

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
        _previousFrameDelta = _nextFrameDelta;
        _previousFrameVelocity = _nextFrameVelocity;

        _nextFramePosition = position;

        _nextFrameDelta = _nextFramePosition - _currentPosition;
        _nextFrameVelocity = _nextFrameDelta / Time.fixedDeltaTime;
    }
}
