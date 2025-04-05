using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerMovement playerMovement;
    private CharacterController controller;
    private InputManager inputManager;
    
    // Animation parameter names
    private const string IS_WALKING = "IsWalking";
    private const string IS_RUNNING = "IsRunning";
    private const string IS_JUMPING = "IsJumping";
    private const string IS_GROUNDED = "IsGrounded";
    private const string IS_STRAFING_LEFT = "IsStrafingLeft";
    private const string IS_STRAFING_RIGHT = "IsStrafingRight";
    private const string IS_BACKWARD = "IsBackward";
    private const string IS_BACKWARD_RUNNING = "IsBackwardRunning"; // Thêm cho chạy lùi
    
    [SerializeField] private float moveThreshold = 0.1f;
    private Vector3 lastPosition;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
        inputManager = GetComponent<InputManager>();
        lastPosition = transform.position;
        
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }
    
    void Update()
    {
        if (animator == null)
        {
            Debug.LogError("Animator is null");
            return;
        }
        
        // Lấy dữ liệu đầu vào từ InputManager
        Vector2 moveInput = Vector2.zero;
        if (inputManager != null && inputManager.onFoot.Movement != null)
        {
            moveInput = inputManager.onFoot.Movement.ReadValue<Vector2>();
        }
        
        // Kiểm tra di chuyển lùi (giá trị Y âm trong Vector2 input)
        bool isBackward = moveInput.y < -moveThreshold;
        
        // Kiểm tra di chuyển ngang (strafe)
        bool isStrafingLeft = moveInput.x < -moveThreshold;
        bool isStrafingRight = moveInput.x > moveThreshold;
        
        // Kiểm tra hướng di chuyển
        bool isMovingForward = moveInput.y > moveThreshold; // Chỉ xét di chuyển tiến
        bool isMovingSideways = isStrafingLeft || isStrafingRight; // Di chuyển ngang
        
        // Tính toán chuyển động tổng thể (đảm bảo không tính chuyển động lùi vào isMoving)
        bool isMoving = (isMovingForward || isMovingSideways);
        bool isMovingForwardOnly = isMoving && !isBackward;
        
        // Xác định trạng thái chạy - cho phép chạy ở mọi hướng
        bool isSprintPressed = inputManager != null && inputManager.onFoot.Sprint.IsPressed();
        bool isRunningForward = isSprintPressed && isMovingForwardOnly;
        bool isRunningBackward = isSprintPressed && isBackward; // Thêm biến riêng cho chạy lùi
        
        bool isJumping = inputManager != null && inputManager.onFoot.Jump.triggered;
        
        // Kiểm tra vị trí thay đổi
        Vector3 currentPosition = transform.position;
        bool hasPositionChanged = Vector3.Distance(lastPosition, currentPosition) > 0.01f;
        lastPosition = currentPosition;
        
        // Debug
        //Debug.Log($"Input: {moveInput}, Forward: {isMovingForward}, Backward: {isBackward}, Running Backward: {isRunningBackward}");
        
        // Gán giá trị cho Animator
        animator.SetBool(IS_WALKING, isMovingForwardOnly);
        animator.SetBool(IS_RUNNING, isRunningForward);
        animator.SetBool(IS_JUMPING, isJumping);
        animator.SetBool(IS_GROUNDED, controller != null && controller.isGrounded);
        animator.SetBool(IS_STRAFING_LEFT, isStrafingLeft && !isBackward);
        animator.SetBool(IS_STRAFING_RIGHT, isStrafingRight && !isBackward);
        
        // Kích hoạt cả hai tham số khi chạy lùi
        animator.SetBool(IS_BACKWARD, isBackward); // Luôn kích hoạt khi đi lùi
        animator.SetBool(IS_BACKWARD_RUNNING, isRunningBackward); // Kích hoạt khi chạy lùi
    }
}
