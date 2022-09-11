using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWalkState : PlayerBaseState
{
    public PlayerWalkState(PlayerStateMachine currentStateMachine, PlayerStateFactory playerStateFactory)
        : base(currentStateMachine, playerStateFactory) { }

    public override void CheckSwitchStates()
    {
        if (!StateMachine.IsMovementPressed)
            SwitchState(Factory.Idle());
        else if (StateMachine.IsMovementPressed && StateMachine.IsRunPressed)
            SwitchState(Factory.Run());
    }

    public override void EnterState()
    {
        StateMachine.Animator.SetBool(StateMachine.IsWalkingHash, true);
        StateMachine.Animator.SetBool(StateMachine.IsRunningHash, false);
    }

    public override void ExitState()
    {

    }

    public override void InitializeSubState()
    {

    }

    public override void UpdateState()
    {
        StateMachine.AppliedMovementX = StateMachine.CurrentMovementInput.x;
        StateMachine.AppliedMovementZ = StateMachine.CurrentMovementInput.y;
        CheckSwitchStates();
    }
}
