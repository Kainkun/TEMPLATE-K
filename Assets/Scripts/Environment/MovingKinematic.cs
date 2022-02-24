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
    private Rigidbody2D _rb;
    private Vector2 _previousFramePosition;
    private Vector2 _nextFramePosition;
    private Vector2 _nextFrameVelocity;
    private Vector2 _previousFrameVelocity;
    private Vector2 _nextFrameDelta;
    private Vector2 _previousFrameDelta;

    public Vector2 StartPosition => _startPosition;
    public Rigidbody2D RigidBody => _rb;
    public Vector2 NextFramePosition => _nextFramePosition;
    public Vector2 PreviousFramePosition => _previousFramePosition;
    public Vector2 NextFrameVelocity => _nextFrameVelocity;
    public Vector2 PreviousFrameVelocity => _previousFrameVelocity;
    public Vector2 NextFrameDelta => _nextFrameDelta;
    public Vector2 PreviousFrameDelta => _previousFrameDelta;

    private void OnValidate()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _startPosition = transform.position;
        _previousFramePosition = _startPosition;
        _nextFramePosition = _startPosition;
    }

    public void MovementUpdate(Vector2 position)
    {
        _previousFramePosition = _nextFramePosition;
        _previousFrameDelta = _nextFrameDelta;
        _previousFrameVelocity = _nextFrameVelocity;
        _rb.MovePosition(_nextFramePosition);

        _nextFramePosition = position;
        _nextFrameDelta = _nextFramePosition - _previousFramePosition;
        _nextFrameVelocity = _nextFrameDelta / Time.fixedDeltaTime;
    }
    
    public void MovementUpdate()
    {
        _previousFramePosition = _nextFramePosition;
        _previousFrameDelta = _nextFrameDelta;
        _previousFrameVelocity = _nextFrameVelocity;
        _rb.MovePosition(_nextFramePosition);

        _nextFrameDelta = _nextFramePosition - _previousFramePosition;
        _nextFrameVelocity = _nextFrameDelta / Time.fixedDeltaTime;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_previousFramePosition, 0.2f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_nextFramePosition, 0.2f);
    }
}
