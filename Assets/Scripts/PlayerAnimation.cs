//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class PlayerAnimation : MonoBehaviour
//{
//    private Animator animator;
//    private PlayerMovement playerMovement;
//    private CharacterController controller;
//    private InputManager inputManager;
    
//    // Animation parameter names
//    private const string IS_WALKING = "IsWalking";
//    private const string IS_RUNNING = "IsRunning";
//    private const string IS_JUMPING = "IsJumping";
//    private const string IS_GROUNDED = "IsGrounded";
//    private const string IS_STRAFING_LEFT = "IsStrafingLeft";
//    private const string IS_STRAFING_RIGHT = "IsStrafingRight";

//    [SerializeField] private float moveThreshold = 0.1f;
//    private Vector3 lastPosition;

//    void Start()
//    {
//        animator = GetComponent<Animator>();
//        playerMovement = GetComponent<PlayerMovement>();
//        controller = GetComponent<CharacterController>();
//        inputManager = GetComponent<InputManager>();
//        lastPosition = transform.position;

//        if (animator == null)
//        {
//            animator = GetComponentInChildren<Animator>();
//        }
//    }

//    void Update()
//    {
//        if (animator == null)
//        {
//            Debug.LogError("Animator is null");
//            return;
//        }

//        // Lấy dữ liệu đầu vào từ InputManager
//        Vector2 moveInput = Vector2.zero;
//        if (inputManager != null && inputManager.onFoot.Movement != null)
//        {
//            moveInput = inputManager.onFoot.Movement.ReadValue<Vector2>();
//        }

//        // Kiểm tra hướng di chuyển
//        bool isMoving = moveInput.magnitude > moveThreshold;
//        bool isRunning = inputManager != null && inputManager.onFoot.Sprint.triggered; // Changed Run to Sprint
//        bool isJumping = inputManager != null && inputManager.onFoot.Jump.triggered;

//        // Kiểm tra di chuyển ngang (strafe)
//        bool isStrafingLeft = moveInput.x < -moveThreshold;
//        bool isStrafingRight = moveInput.x > moveThreshold;

//        // Kiểm tra vị trí thay đổi
//        Vector3 currentPosition = transform.position;
//        bool hasPositionChanged = Vector3.Distance(lastPosition, currentPosition) > 0.01f;
//        lastPosition = currentPosition;

//        // Ghi log kiểm tra
//        Debug.Log($"Input: {moveInput}, Moving: {isMoving}, Strafe Left: {isStrafingLeft}, Strafe Right: {isStrafingRight}");

//        // Gán giá trị cho Animator
//        animator.SetBool(IS_WALKING, isMoving || hasPositionChanged);
//        animator.SetBool(IS_RUNNING, isRunning);
//        animator.SetBool(IS_JUMPING, isJumping);
//        animator.SetBool(IS_GROUNDED, controller != null && controller.isGrounded);
//        animator.SetBool(IS_STRAFING_LEFT, isStrafingLeft);
//        animator.SetBool(IS_STRAFING_RIGHT, isStrafingRight);
//    }
//}
