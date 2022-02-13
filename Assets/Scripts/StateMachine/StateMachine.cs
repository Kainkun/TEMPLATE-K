using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StateMachine : MonoBehaviour
{
    public State<StateMachine> currentState;

    public void SetState(State<StateMachine> state)
    {
        currentState = state;
        currentState.EnterState(this);
    }
}