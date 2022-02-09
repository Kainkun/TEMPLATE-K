using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class MovingKinematic : MonoBehaviour
{
    protected Vector2 startPosition;
    protected Rigidbody2D rb;
    protected Vector2 previousPosition;
    [HideInInspector]
    public Vector2 velocity;
    [HideInInspector]
    public Vector2 delta;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    void FixedUpdate()
    {
        previousPosition = rb.position;
        Vector2 newPosition = Move();
        delta = newPosition - previousPosition;
        velocity = delta / Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    protected abstract Vector2 Move();
}
