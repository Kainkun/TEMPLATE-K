using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class State<T>
{
    public virtual void EnterState(T stateMachine) { }
    public virtual void UpdateState(T stateMachine) { }
    public virtual void ExitState(T stateMachine) { }
} 