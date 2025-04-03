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

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = CheckIfGrounded();
        Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        ProcessMove(moveInput);
        Jump();
    }

    public void ProcessMove(Vector2 input)
    {
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;

        // Áp dụng hệ số tốc độ sprint nếu đang sprint
        float currentSpeed = isSprinting ? playerSpeed * sprintSpeedMultiplier : playerSpeed;

        controller.Move(transform.TransformDirection(moveDirection) * Time.deltaTime * currentSpeed);
        playerVelocity.y += playerGravity * Time.deltaTime;
        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -0.1f;
        }
        controller.Move(playerVelocity * Time.deltaTime);
    }
    private bool CheckIfGrounded()
        {
            float groundCheckDistance = 0.2f;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f; // Xuất phát từ nhân vật
            bool grounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance);

            Debug.DrawRay(rayStart, Vector3.down * groundCheckDistance, grounded ? Color.green : Color.red);
            return grounded;
        }
    public void Jump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            //the equation for jumping
            playerVelocity.y = Mathf.Sqrt(playerJumpHeight * -3f * playerGravity);
        }
     
    }

    // Phương thức mới để xử lý trạng thái sprint
    public void Sprint(bool sprintState)
    {
        isSprinting = sprintState;
    }
}
