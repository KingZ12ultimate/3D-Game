using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerBaseState, IRootState
{
    public PlayerJumpState(PlayerStateMachine currentStateMachine, PlayerStateFactory playerStateFactory)
        : base(currentStateMachine, playerStateFactory)
    {
        IsRootState = true;
        InitializeSubState();
    }

    IEnumerator JumpResetRoutine()
    {
        yield return new WaitForSeconds(StateMachine.JumpResetTime);
        StateMachine.JumpCount = 0;
    }

    public override void CheckSwitchStates()
    {
        if (StateMachine.CharacterController.isGrounded)
            SwitchState(Factory.Grounded());
    }

    public override void EnterState()
    {
        HandleJump();
    }

    public override void ExitState()
    {
        StateMachine.Animator.SetBool(StateMachine.IsJumpingHash, false);
        if (StateMachine.IsJumpPressed)
            StateMachine.RequireNewJumpPress = true;
        StateMachine.CurrentJumpResetRoutine = StateMachine.StartCoroutine(JumpResetRoutine());
        if (StateMachine.JumpCount == 3)
        {
            StateMachine.JumpCount = 0;
            StateMachine.Animator.SetInteger(StateMachine.JumpCountHash, StateMachine.JumpCount);
        }
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

    void HandleJump()
    {
        if (StateMachine.JumpCount < 3 && StateMachine.CurrentJumpResetRoutine != null)
        {
            StateMachine.StopCoroutine(StateMachine.CurrentJumpResetRoutine);
        }
        StateMachine.Animator.SetBool(StateMachine.IsJumpingHash, true);
        StateMachine.JumpCount++;
        StateMachine.Animator.SetInteger(StateMachine.JumpCountHash, StateMachine.JumpCount);
        StateMachine.CurrentMovementY = StateMachine.InitialJumpVelocities[StateMachine.JumpCount];
        StateMachine.AppliedMovementY = StateMachine.InitialJumpVelocities[StateMachine.JumpCount];
    }

    public void HandleGravity()
    {
        bool isFalling = StateMachine.CurrentMovementY <= 0.0f || !StateMachine.IsJumpPressed;
        float fallMultiplier = 2.0f;

        if (isFalling)
        {
            float previousYVelocity = StateMachine.CurrentMovementY;
            StateMachine.CurrentMovementY += StateMachine.JumpGravities[StateMachine.JumpCount] * fallMultiplier * Time.deltaTime;
            StateMachine.AppliedMovementY = Mathf.Max((previousYVelocity + StateMachine.CurrentMovementY) * 0.5f, -20.0f);
        }
        else
        {
            float previousYVelocity = StateMachine.CurrentMovementY;
            StateMachine.CurrentMovementY += StateMachine.JumpGravities[StateMachine.JumpCount] * Time.deltaTime;
            StateMachine.AppliedMovementY = Mathf.Max((previousYVelocity + StateMachine.CurrentMovementY) * 0.5f, -20.0f);
        }
    }
}
