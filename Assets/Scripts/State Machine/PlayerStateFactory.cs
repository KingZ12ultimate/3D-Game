using System.Collections.Generic;

enum PlayerStates
{
    idle,
    walk,
    run,
    grounded,
    jump,
    fall
}

public class PlayerStateFactory
{
    PlayerStateMachine stateMachine;
    Dictionary<PlayerStates, PlayerBaseState> states = new Dictionary<PlayerStates, PlayerBaseState>();

    public PlayerStateFactory(PlayerStateMachine _currentStateMachine)
    {
        stateMachine = _currentStateMachine;
        states[PlayerStates.idle] = new PlayerIdleState(stateMachine, this);
        states[PlayerStates.walk] = new PlayerWalkState(stateMachine, this);
        states[PlayerStates.run] = new PlayerRunState(stateMachine, this);
        states[PlayerStates.jump] = new PlayerJumpState(stateMachine, this);
        states[PlayerStates.grounded] = new PlayerGroundedState(stateMachine, this);
        states[PlayerStates.fall] = new PlayerFallState(stateMachine, this);
    }

    #region Factory Functions
    /// <summary>
    /// Returns a new state instance of type <c>PlayerIdleState</c>.
    /// </summary>
    public PlayerBaseState Idle()
    {
        return states[PlayerStates.idle];
    }
    /// <summary>
    /// Returns a new state instance of type <c>PlayerWalkState</c>.
    /// </summary>
    public PlayerBaseState Walk()
    {
        return states[PlayerStates.walk];
    }
    /// <summary>
    /// Returns a new state instance of type <c>PlayerRunState</c>.
    /// </summary>
    public PlayerBaseState Run()
    {
        return states[PlayerStates.run];
    }
    /// <summary>
    /// Returns a new state instance of type <c>PlayerJumpState</c>.
    /// </summary>
    public PlayerBaseState Jump()
    {
        return states[PlayerStates.jump];
    }
    /// <summary>
    /// Returns a new state instance of type <c>PlayerGroundedState</c>.
    /// </summary>
    public PlayerBaseState Grounded()
    {
        return states[PlayerStates.grounded];
    }
    /// <summary>
    /// Returns a new state instance of type <c>PlayerFallState</c>.
    /// </summary>
    public PlayerBaseState Fall()
    {
        return states[PlayerStates.fall];
    }
    #endregion
}
