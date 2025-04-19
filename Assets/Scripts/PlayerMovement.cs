using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    public float playerSpeed = 5.0f;
    public float sprintSpeedMultiplier = 10.5f; // Hệ số tăng tốc khi sprint
    public float playerGravity = -9.81f;
    public float playerJumpHeight = 1.0f;
    private bool isGrounded;
    private bool isSprinting = false; // Trạng thái sprint
    
    // Tham chiếu đến HealthManager để xử lý thể lực
    private HealthManager healthManager;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        healthManager = GetComponent<HealthManager>();
        
        // Nếu không tìm thấy HealthManager trên đối tượng này, tìm kiếm trong toàn bộ player
        if (healthManager == null)
        {
            healthManager = GetComponentInChildren<HealthManager>();
            
            if (healthManager == null && transform.parent != null)
            {
                healthManager = transform.parent.GetComponent<HealthManager>();
            }
            
            if (healthManager == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null && playerObject != this.gameObject)
                {
                    healthManager = playerObject.GetComponent<HealthManager>();
                }
            }
            
            if (healthManager == null)
            {
                Debug.LogWarning("Không thể tìm thấy HealthManager cho PlayerMovement. Hệ thống thể lực sẽ không hoạt động!");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = controller.isGrounded;
    }

    public void ProcessMove(Vector2 input)
    {
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;
        
        bool isMoving = moveDirection.magnitude > 0.1f;
        
        // Kiểm tra nếu người chơi đang di chuyển và đang muốn chạy nhanh
        if (isMoving && isSprinting)
        {
            // Kiểm tra nếu có đủ thể lực để chạy nhanh
            if (healthManager != null && healthManager.UseStamina(healthManager.staminaDepletionRate))
            {
                // Áp dụng tốc độ sprint
                controller.Move(transform.TransformDirection(moveDirection) * Time.deltaTime * playerSpeed * sprintSpeedMultiplier);
            }
            else
            {
                // Nếu không đủ thể lực, trở về tốc độ đi bộ bình thường
                isSprinting = false;
                controller.Move(transform.TransformDirection(moveDirection) * Time.deltaTime * playerSpeed);
            }
        }
        else
        {
            // Di chuyển bình thường khi không sprint
            controller.Move(transform.TransformDirection(moveDirection) * Time.deltaTime * playerSpeed);
        }

        playerVelocity.y += playerGravity * Time.deltaTime;
        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        
        controller.Move(playerVelocity * Time.deltaTime);
    }

    public void Jump()
    {
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = Mathf.Sqrt(playerJumpHeight * -3f * playerGravity);
        }
    }

    // Phương thức xử lý trạng thái sprint
    public void Sprint(bool sprintState)
    {
        // Chỉ cho phép sprint khi có đủ thể lực
        if (sprintState && healthManager != null && healthManager.currentStamina > 0)
        {
            isSprinting = true;
        }
        else
        {
            isSprinting = false;
        }
    }
}
