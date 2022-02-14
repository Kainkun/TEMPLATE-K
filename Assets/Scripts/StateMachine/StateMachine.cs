using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StateMachine<T> : MonoBehaviour where T : StateMachine<T>
{
    public State<T> currentState;

    public void SetState(State<T> state)
    {
        currentState?.ExitState((T)this);
        currentState = state;
        currentState.EnterState((T)this);
    }
}