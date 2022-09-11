using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // Component variables
    Animator animator;
    CharacterController characterController;
    PlayerInput playerInput;

    // Variables for player input data
    bool isMovementPressed;
    bool isRunPressed;
    Vector2 currentMovementInput;
    Vector3 currentMovement;
    Vector3 currentRunMovement;
    Vector3 appliedMovement;

    float rotationFactorPerFrame = 1.0f;
    float runMultiplier = 3.0f;
    float gravity = -9.8f;
    float groundedGravity = -9.8f;

    // Animation hashes
    int isWalkingHash;
    int isRunningHash;
    int isJumpingHash;
    int jumpCountHash;

    // Jumping variables
    bool isJumpPressed = false;
    bool isJumping = false;
    float initialJumpVelocity;
    public float maxJumpHeight = 4.0f;
    public float maxJumpTime = 0.75f;
    float jumpResetTime = 0.5f;
    int jumpCount = 0;
    Dictionary<int, float> initialJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float> jumpGravities = new Dictionary<int, float>();
    Coroutine currentJumpResetRoutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        playerInput = new PlayerInput();

        // Animator hash variables
        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        isJumpingHash = Animator.StringToHash("isJumping");
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

        jumpGravities.Add(0, groundedGravity);
        jumpGravities.Add(1, gravity);
        jumpGravities.Add(2, secondJumpGravity);
        jumpGravities.Add(3, thirdJumpGravity);
    }

    void HandleJump()
    {
        if (!isJumping && characterController.isGrounded && isJumpPressed)
        {
            if (jumpCount < 3 && currentJumpResetRoutine != null)
                StopCoroutine(currentJumpResetRoutine);
            animator.SetBool(isJumpingHash, true);
            isJumping = true;
            jumpCount++;
            animator.SetInteger(jumpCountHash, jumpCount);
            currentMovement.y = initialJumpVelocities[jumpCount];
            appliedMovement.y = initialJumpVelocities[jumpCount];
        }
        else if (!isJumpPressed && isJumping && characterController.isGrounded)
            isJumping = false;
    }

    IEnumerator JumpResetRoutine()
    {
        yield return new WaitForSeconds(jumpResetTime);
        jumpCount = 0;
    }

    #region Callback Functions
    private void OnJump(InputAction.CallbackContext context)
    {
        isJumpPressed = context.ReadValueAsButton();
    }

    private void OnRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
        currentMovement = new Vector3(currentMovementInput.x, 0f, currentMovementInput.y);
        currentRunMovement = currentMovement * runMultiplier;
        isMovementPressed = currentMovementInput != Vector2.zero;
    }
    #endregion

    void HandleRotation()
    {
        Vector3 positionToLookAt = new Vector3(currentMovement.x, 0f, currentMovement.z);
        Quaternion currentRotation = transform.rotation;
        if (isMovementPressed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFactorPerFrame);
        }
    }

    void HandleAnimation()
    {
        bool isWalking = animator.GetBool(isWalkingHash);
        bool isRunning = animator.GetBool(isRunningHash);

        if (isMovementPressed && !isWalking)
            animator.SetBool(isWalkingHash, true);

        if (!isMovementPressed && isWalking)
            animator.SetBool(isWalkingHash, false);

        if ((isMovementPressed && isRunPressed) && !isRunning)
            animator.SetBool(isRunningHash, true);

        if ((!isMovementPressed || !isRunPressed) && isRunning)
            animator.SetBool(isRunningHash, false);
    }

    void HandleGravity()
    {
        bool isFalling = currentMovement.y <= 0.0f || !isJumpPressed;
        float fallMultiplier = 2.0f;

        if (isFalling)
        {
            float previousYVelocity = currentMovement.y;
            currentMovement.y += jumpGravities[jumpCount] * fallMultiplier * Time.deltaTime;
            appliedMovement.y = Mathf.Max((previousYVelocity + currentMovement.y) * 0.5f, -20.0f);
        }
        else
        {
            float previousYVelocity = currentMovement.y;
            currentMovement.y += jumpGravities[jumpCount] * Time.deltaTime;
            appliedMovement.y = Mathf.Max((previousYVelocity + currentMovement.y) * 0.5f, -20.0f);
        }
    }

    void Update()
    {
        HandleRotation();
        HandleAnimation();

        if (isRunPressed)
        {
            appliedMovement.x = currentRunMovement.x;
            appliedMovement.z = currentRunMovement.z;
        }
        else
        {
            appliedMovement.x = currentMovement.x;
            appliedMovement.z = currentMovement.z;
        }

        appliedMovement.x = isRunPressed ? currentMovementInput.x * runMultiplier : currentMovementInput.x;
        appliedMovement.z = isRunPressed ? currentMovementInput.y * runMultiplier : currentMovementInput.y;
        characterController.Move(appliedMovement * Time.deltaTime);

        HandleGravity();
        HandleJump();
    }

    private void OnEnable()
    {
        playerInput.CharacterControls.Enable();
    }

    private void OnDisable()
    {
        playerInput.CharacterControls.Disable();
    }
}
