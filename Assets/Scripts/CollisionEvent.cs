using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CollisionEvent : MonoBehaviour
{
    public UnityEvent<Collision2D> onCollisionEnter;
    public UnityEvent<Collision2D> onCollisionExit;
    public UnityEvent<Collider2D> onTriggerEnter;
    public UnityEvent<Collider2D> onTriggerExit;

    private void OnCollisionEnter2D(Collision2D other)
    {
        onCollisionEnter?.Invoke(other);
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        onCollisionExit?.Invoke(other);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        onTriggerEnter?.Invoke(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        onTriggerExit?.Invoke(other);
    }
}
