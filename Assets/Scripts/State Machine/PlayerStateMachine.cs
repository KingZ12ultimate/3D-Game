using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    #region Player State Machine Data
    // Component variables
    Animator animator;
    CharacterController characterController;
    PlayerInput playerInput;

    // Variables for player input data
    bool isMovementPressed;
    bool isRunPressed;
    Vector2 currentMovementInput;
    Vector3 currentMovement;
    Vector3 appliedMovement;

    float rotationFactorPerFrame = 1.0f;
    float runMultiplier = 6.0f;
    float gravity = -9.8f;

    // Animation hashes
    int isWalkingHash;
    int isRunningHash;
    int isJumpingHash;
    int isFallingHash;
    int jumpCountHash;
    bool requireNewJumpPress = false;

    // Jumping variables
    bool isJumpPressed = false;
    // bool isJumping = false;
    float initialJumpVelocity;
    public float maxJumpHeight = 4.0f;
    public float maxJumpTime = 0.75f;
    float jumpResetTime = 0.5f;
    int jumpCount = 0;
    Dictionary<int, float> initialJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float> jumpGravities = new Dictionary<int, float>();
    Coroutine currentJumpResetRoutine;

    // State Machine variables
    PlayerBaseState currentState;
    PlayerStateFactory states;
    #endregion

    #region Getters And Setters (Properties)
    public PlayerBaseState CurrentState { get { return currentState; } set { currentState = value; } }
    public Animator Animator { get { return animator; } }
    public CharacterController CharacterController { get { return characterController; } }
    public Coroutine CurrentJumpResetRoutine { get { return currentJumpResetRoutine; } set { currentJumpResetRoutine = value; } }
    public Dictionary<int, float> InitialJumpVelocities { get { return initialJumpVelocities; } }
    public Dictionary<int, float> JumpGravities { get { return jumpGravities; } }
    public Vector2 CurrentMovementInput { get { return currentMovementInput; } }
    public int JumpCount { get { return jumpCount; } set { jumpCount = value; } }
    public int IsWalkingHash { get { return isWalkingHash; } }
    public int IsRunningHash { get { return isRunningHash; } }
    public int IsJumpingHash { get { return isJumpingHash; } }
    public int IsFallingHash { get { return isFallingHash; } }
    public int JumpCountHash { get { return jumpCountHash; } }
    public bool RequireNewJumpPress { get { return requireNewJumpPress; } set { requireNewJumpPress = value; } }
    public bool IsMovementPressed { get { return isMovementPressed; } set { isMovementPressed = value; } }
    public bool IsRunPressed { get { return isRunPressed; } set { isRunPressed = value; } }
    public bool IsJumpPressed { get { return isJumpPressed; } }
    public float JumpResetTime { get { return jumpResetTime; } }
    public float RunMultiplier { get { return runMultiplier; } }
    public float Gravity { get { return gravity; } }
    public float CurrentMovementY { get { return currentMovement.y; } set { currentMovement.y = value; } }
    public float AppliedMovementX { get { return appliedMovement.x; } set { appliedMovement.x = value; } }
    public float AppliedMovementY { get { return appliedMovement.y; } set { appliedMovement.y = value; } }
    public float AppliedMovementZ { get { return appliedMovement.z; } set { appliedMovement.z = value; } }
    #endregion

    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        playerInput = new PlayerInput();

        states = new PlayerStateFactory(this);
        currentState = states.Grounded();
        currentState.EnterState();

        // Animator hash variables
        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        isJumpingHash = Animator.StringToHash("isJumping");
        isFallingHash = Animator.StringToHash("isFalling");
        jumpCountHash = Animator.StringToHash("jumpCount");

        playerInput.CharacterControls.Move.performed += OnMove;
        playerInput.CharacterControls.Move.canceled += OnMove;
        playerInput.CharacterControls.Run.started += OnRun;
        playerInput.CharacterControls.Run.canceled += OnRun;
        playerInput.CharacterControls.Jump.started += OnJump;
        playerInput.CharacterControls.Jump.canceled += OnJump;

        SetupJumpVariables();
    }

    private void SetupJumpVariables()
    {
        float timeToApex = maxJumpTime / 2;
        gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
        float secondJumpGravity = (-2 * (maxJumpHeight + 2)) / Mathf.Pow(timeToApex * 1.25f, 2);
        float secondJumpInitialVelocity = (2 * (maxJumpHeight + 2)) / (timeToApex * 1.25f);
        float thirdJumpGravity = (-2 * (maxJumpHeight + 4)) / Mathf.Pow(timeToApex * 1.5f, 2);
        float thirdJumpInitialVelocity = (2 * (maxJumpHeight + 4)) / (timeToApex * 1.5f);

        initialJumpVelocities.Add(1, initialJumpVelocity);
        initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

        jumpGravities.Add(0, gravity);
        jumpGravities.Add(1, gravity);
        jumpGravities.Add(2, secondJumpGravity);
        jumpGravities.Add(3, thirdJumpGravity);
    }

    void Update()
    {
        HandleRotation();
        currentState.UpdateStates();
        // Debug.Log("Super State: " + currentState + ", SubState: " + currentState.CurrentSubState);

        characterController.Move(appliedMovement * Time.deltaTime);
    }

    void HandleRotation()
    {
        Vector3 positionToLookAt = new Vector3(currentMovementInput.x, 0f, currentMovementInput.y);
        Quaternion currentRotation = transform.rotation;
        if (isMovementPressed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFactorPerFrame);
        }
    }

    #region Callback Functions
    private void OnJump(InputAction.CallbackContext context)
    {
        isJumpPressed = context.ReadValueAsButton();
        requireNewJumpPress = false;
    }

    private void OnRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
        isMovementPressed = currentMovementInput != Vector2.zero;
    }
    #endregion

    #region Enable / Disable
    private void OnEnable()
    {
        playerInput.CharacterControls.Enable();
    }

    private void OnDisable()
    {
        playerInput.CharacterControls.Disable();
    }
    #endregion
}
