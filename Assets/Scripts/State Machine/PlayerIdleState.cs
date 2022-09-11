using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerBaseState
{
    public PlayerIdleState(PlayerStateMachine currentStateMachine, PlayerStateFactory playerStateFactory)
        : base(currentStateMachine, playerStateFactory) { }

    public override void CheckSwitchStates()
    {
        if (StateMachine.IsMovementPressed && StateMachine.IsRunPressed)
            SwitchState(Factory.Run());
        else if (StateMachine.IsMovementPressed)
            SwitchState(Factory.Walk());
    }

    public override void EnterState()
    {
        StateMachine.Animator.SetBool(StateMachine.IsWalkingHash, false);
        StateMachine.Animator.SetBool(StateMachine.IsRunningHash, false);
        StateMachine.AppliedMovementX = 0f;
        StateMachine.AppliedMovementZ = 0f;
    }

    public override void ExitState()
    {

    }

    public override void InitializeSubState()
    {

    }

    public override void UpdateState()
    {
        CheckSwitchStates();
    }
}
