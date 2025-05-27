using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Scripts.API; // Add this using statement

public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    public PlayerInput.OnFootActions onFoot;
    private PlayerMovement movement;
    private PlayerLook look;
    private WeaponManager weaponManager;

    // Start is called before the first frame update
    void Start()
    {
    }
    void Awake()
    {
        playerInput = new PlayerInput();
        onFoot = playerInput.OnFoot;
        movement = GetComponent<PlayerMovement>();
        look = GetComponent<PlayerLook>();
        weaponManager = GetComponent<WeaponManager>();
        
        onFoot.Jump.performed += ctx => movement.Jump();
        
        // Thêm xử lý sự kiện Sprint
        onFoot.Sprint.performed += ctx => movement.Sprint(true);
        onFoot.Sprint.canceled += ctx => movement.Sprint(false);
        
        // Thêm callbacks cho nhặt và vứt vũ khí nếu có WeaponManager
        if (weaponManager != null)
        {
            // Có thể thêm nút riêng trong InputSystem nếu cần
            // Hoặc sử dụng các phím mặc định qua Update()
        }
    }

    void FixedUpdate()
    {
        // Don't process movement if game is paused
        if (!PauseMenu.IsGamePaused)
        {
            movement.ProcessMove(onFoot.Movement.ReadValue<Vector2>());
        }
    }
    
    void LateUpdate()
    {
        // Don't process look if game is paused
        if (!PauseMenu.IsGamePaused)
        {
            look.ProcessLook(onFoot.Look.ReadValue<Vector2>());
        }
    }
    
    void Update()
    {
        // Only process weapon input if game is not paused
        if (!PauseMenu.IsGamePaused)
        {
            // Xử lý nhặt/vứt vũ khí thông qua WeaponManager
            // Việc này đã được xử lý trong WeaponManager.Update()
        }
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
