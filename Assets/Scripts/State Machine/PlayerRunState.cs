using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunState : PlayerBaseState
{
    public PlayerRunState(PlayerStateMachine currentStateMachine, PlayerStateFactory playerStateFactory)
        : base(currentStateMachine, playerStateFactory) { }

    public override void CheckSwitchStates()
    {
        if (!StateMachine.IsMovementPressed)
            SwitchState(Factory.Idle());
        else if (StateMachine.IsMovementPressed && !StateMachine.IsRunPressed)
            SwitchState(Factory.Walk());
    }

    public override void EnterState()
    {
        StateMachine.Animator.SetBool(StateMachine.IsWalkingHash, true);
        StateMachine.Animator.SetBool(StateMachine.IsRunningHash, true);
    }

    public override void ExitState()
    {

    }

    public override void InitializeSubState()
    {

    }

    public override void UpdateState()
    {
        StateMachine.AppliedMovementX = StateMachine.CurrentMovementInput.x * StateMachine.RunMultiplier;
        StateMachine.AppliedMovementZ = StateMachine.CurrentMovementInput.y * StateMachine.RunMultiplier;
        CheckSwitchStates();
    }
}
