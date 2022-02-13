using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class MovingKinematic : MonoBehaviour
{
    protected Vector2 startPosition;
    private Rigidbody2D rb;
    private Vector2 currentPosition;
    private Vector2 nextFramePosition;
    private Vector2 _velocity;
    private Vector2 _delta;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        currentPosition = startPosition;
        nextFramePosition = startPosition;
    }

    void FixedUpdate()
    {
        rb.MovePosition(nextFramePosition);
        currentPosition = nextFramePosition;
        
        Move(ref nextFramePosition);
        _delta = nextFramePosition - currentPosition;
        _velocity = _delta / Time.fixedDeltaTime;
    }

    protected abstract void Move(ref Vector2 nextFramePosition);

    public Vector2 Velocity => _velocity;
    public Vector2 Delta => _delta;
}
