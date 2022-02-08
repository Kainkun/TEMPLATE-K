using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Grapple : MonoBehaviour
{
    public Transform mark;
    public float pullSpeed = 1;
    private DistanceJoint2D _distanceJoint;

    private void Start()
    {
        _distanceJoint = gameObject.AddComponent<DistanceJoint2D>();
        _distanceJoint.autoConfigureDistance = false;
        _distanceJoint.enabled = false;
    }

    private void OnEnable()
    {
        //GetComponent<PlatformerController>().AddInterrupter(this, 1);
    }

    private void OnDisable()
    {
        //GetComponent<PlatformerController>().RemoveInterrupter(this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            _distanceJoint.enabled = true;
            _distanceJoint.connectedAnchor = mark.position;
            _distanceJoint.distance = Vector2.Distance(mark.position, transform.position);
        }

        if (Input.GetKey(KeyCode.F))
        {
            _distanceJoint.distance -= Time.deltaTime * pullSpeed;
        }
        
        if (Input.GetKeyUp(KeyCode.F))
        {
            _distanceJoint.enabled = false;
        }


    }
}
