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
    
    // Mảng âm thanh để hỗ trợ nhiều clip
    public AudioSource[] walkSounds; // Mảng âm thanh bước chân đi bộ
    public AudioSource[] runSounds;  // Mảng âm thanh bước chân chạy
    public AudioSource jumpSound; // Âm thanh nhảy
    
    // Thêm biến điều khiển âm thanh bước chân
    public float footstepInterval = 0.5f; // Khoảng thời gian giữa các bước chân khi đi bộ
    public float runFootstepInterval = 0.3f; // Khoảng thời gian giữa các bước chân khi chạy
    private float footstepTimer = 0f;
    
    // Tham chiếu đến HealthManager để xử lý thể lực
    private HealthManager healthManager;

    // Start is called before the first frame update
    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
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

    // Phát âm thanh bước chân đi bộ ngẫu nhiên
    private void PlayRandomWalkSound()
    {
        if (walkSounds != null && walkSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, walkSounds.Length);
            AudioSource selectedSound = walkSounds[randomIndex];
            
            if (selectedSound != null)
            {
                selectedSound.pitch = Random.Range(0.9f, 1.1f); // Thay đổi pitch cho đa dạng
                selectedSound.Play();
            }
        }
    }
    
    // Phát âm thanh bước chân chạy ngẫu nhiên
    private void PlayRandomRunSound()
    {
        if (runSounds != null && runSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, runSounds.Length);
            AudioSource selectedSound = runSounds[randomIndex];
            
            if (selectedSound != null)
            {
                selectedSound.pitch = Random.Range(0.9f, 1.1f); // Thay đổi pitch cho đa dạng
                selectedSound.Play();
            }
        }
    }

    public void ProcessMove(Vector2 input)
    {
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;
        
        bool isMoving = moveDirection.magnitude > 0.1f;
        
        // Xử lý âm thanh bước chân
        if (isMoving && isGrounded)
        {
            footstepTimer -= Time.deltaTime;
            
            if (footstepTimer <= 0)
            {
                // Đặt lại thời gian dựa vào tốc độ di chuyển
                if (isSprinting && healthManager != null && healthManager.currentStamina > 0)
                {
                    footstepTimer = runFootstepInterval;
                    PlayRandomRunSound();
                }
                else
                {
                    footstepTimer = footstepInterval;
                    PlayRandomWalkSound();
                }
            }
        }
        
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
            
            // Phát âm thanh nhảy nếu có
            if (jumpSound != null)
            {
                jumpSound.Play();
            }
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
