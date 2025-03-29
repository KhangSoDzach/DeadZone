using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    private PlayerInput.OnFootActions onFoot;
    private PlayerMovement movement;
    private PlayerLook look;
    // Start is called before the first frame update
    void Awake()
    {
        playerInput = new PlayerInput();
        onFoot = playerInput.OnFoot;
        movement = GetComponent<PlayerMovement>();
        look = GetComponent<PlayerLook>();
        onFoot.Jump.performed += ctx => movement.Jump();
    }
    // void Start()
    // {
    //     playerInput = new PlayerInput();
    //     onFoot = playerInput.OnFoot;
    //     movement = GetComponent<PlayerMovement>();

    // }

    // Update is called once per frame
    void FixedUpdate()
    {
        movement.ProcessMove(onFoot.Movement.ReadValue<Vector2>());
    }
    void LateUpdate()
    {
        look.ProcessLook(onFoot.Look.ReadValue<Vector2>());
    }
    private void OnEnable()
    {
        onFoot.Enable();
    }
    private void OnDisable()
    {
        onFoot.Disable();
    }
}
