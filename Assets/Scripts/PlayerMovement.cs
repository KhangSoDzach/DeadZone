using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    public float playerSpeed = 5.0f;
    public float playerGravity = -9.81f;
    public float playerJumpHeight = 1.0f;
    private bool isGrounded;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = controller.isGrounded;
    }
    public void ProcessMove(Vector2 input){
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;
        controller.Move( transform.TransformDirection(moveDirection) * Time.deltaTime * playerSpeed);
        playerVelocity.y += playerGravity * Time.deltaTime;
        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        controller.Move(playerVelocity * Time.deltaTime);

    }
    public void Jump(){
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = Mathf.Sqrt(playerJumpHeight * -3f * playerGravity);
        }

    }
}
