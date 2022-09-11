using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundedState : PlayerBaseState, IRootState
{
    public PlayerGroundedState(PlayerStateMachine currentStateMachine, PlayerStateFactory playerStateFactory)
        : base(currentStateMachine, playerStateFactory)
    {
        IsRootState = true;
        InitializeSubState();
    }

    public void HandleGravity()
    {
        StateMachine.CurrentMovementY = StateMachine.Gravity;
        StateMachine.AppliedMovementY = StateMachine.Gravity;
    }

    public override void CheckSwitchStates()
    {
        if (StateMachine.IsJumpPressed && !StateMachine.RequireNewJumpPress)
            SwitchState(Factory.Jump());
        else if (!StateMachine.CharacterController.isGrounded)
            SwitchState(Factory.Fall());
    }

    public override void EnterState()
    {
        HandleGravity();
    }

    public override void ExitState()
    {
        
    }

    public override void InitializeSubState()
    {
        if (StateMachine.IsMovementPressed && StateMachine.IsRunPressed)
            SetSubState(Factory.Run());
        else if (StateMachine.IsMovementPressed && !StateMachine.IsRunPressed)
            SetSubState(Factory.Walk());
        else
            SetSubState(Factory.Idle());
    }

    public override void UpdateState()
    {
        CheckSwitchStates();
    }
}
