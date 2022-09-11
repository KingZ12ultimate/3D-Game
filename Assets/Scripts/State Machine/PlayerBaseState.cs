using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerBaseState
{
    private bool isRootState = false;
    private PlayerStateMachine stateMachine;
    private PlayerStateFactory factory;
    private PlayerBaseState currentSubState;
    private PlayerBaseState currentSuperState;

    protected bool IsRootState { get { return isRootState; } set { isRootState = value; } }
    protected PlayerStateMachine StateMachine { get { return stateMachine; } }
    protected PlayerStateFactory Factory { get { return factory; } }
    public PlayerBaseState CurrentSubState { get { return currentSubState; } }

    public PlayerBaseState(PlayerStateMachine currentStateMachine, PlayerStateFactory playerStateFactory)
    {
        stateMachine = currentStateMachine;
        factory = playerStateFactory;
    }

    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void UpdateState();
    public abstract void CheckSwitchStates();
    public abstract void InitializeSubState();
    public void UpdateStates()
    {
        UpdateState();
        if (currentSubState != null)
            currentSubState.UpdateStates();
    }
    protected void SwitchState(PlayerBaseState newState)
    {
        ExitState();
        newState.EnterState();
        if (isRootState)
        {
            stateMachine.CurrentState.currentSubState.SetSuperState(newState);
            stateMachine.CurrentState = newState;
        }
        else if (currentSuperState != null)
            currentSuperState.SetSubState(newState);
    }
    protected void SetSuperState(PlayerBaseState newSuperState)
    {
        currentSuperState = newSuperState;
    }
    protected void SetSubState(PlayerBaseState newSubState)
    {
        currentSubState = newSubState;
        newSubState.SetSuperState(this);
    }
}
