using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFallState : PlayerBaseState, IRootState
{
    public PlayerFallState(PlayerStateMachine currentStateMachine, PlayerStateFactory playerStateFactory)
        : base(currentStateMachine, playerStateFactory)
    {
        IsRootState = true;
        InitializeSubState();
    }

    public override void CheckSwitchStates()
    {
        if (StateMachine.CharacterController.isGrounded)
            SwitchState(Factory.Grounded());
    }

    public override void EnterState()
    {
        StateMachine.Animator.SetBool(StateMachine.IsFallingHash, true);
    }

    public override void ExitState()
    {
        StateMachine.Animator.SetBool(StateMachine.IsFallingHash, false);
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
        HandleGravity();
        CheckSwitchStates();
    }

    public void HandleGravity()
    {
        float previousYVelocity = StateMachine.CurrentMovementY;
        StateMachine.CurrentMovementY += StateMachine.Gravity * Time.deltaTime;
        StateMachine.AppliedMovementY = Mathf.Max((previousYVelocity + StateMachine.CurrentMovementY) * 0.5f, -20.0f);
    }
}
