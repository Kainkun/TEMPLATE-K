using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CollisionEvent : MonoBehaviour
{
    public LayerMask collisionLayer = ~0;
    public LayerMask triggerLayer = ~0;
    
    public UnityEvent<Collision2D> onCollisionEnter;
    public UnityEvent<Collision2D> onCollisionExit;
    public UnityEvent<Collider2D> onTriggerEnter;
    public UnityEvent<Collider2D> onTriggerExit;

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (collisionLayer == (collisionLayer | (1 << other.gameObject.layer)))
            onCollisionEnter?.Invoke(other);
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (collisionLayer == (collisionLayer | (1 << other.gameObject.layer)))
            onCollisionExit?.Invoke(other);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerLayer == (triggerLayer | (1 << other.gameObject.layer)))
            onTriggerEnter?.Invoke(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (triggerLayer == (triggerLayer | (1 << other.gameObject.layer)))
            onTriggerExit?.Invoke(other);
    }
}
