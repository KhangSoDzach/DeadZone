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
    
    // Variable to prevent duplicate InputManager
    private static InputManager instance;
    
    // Start is called before the first frame update
    void Start()
    {
    }
    
    void Awake()
    {
        // Check scene context first
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        if (sceneName.Contains("menu") || sceneName.Contains("login"))
        {
            Debug.Log("InputManager: Skipping initialization in menu scene");
            this.enabled = false;
            return;
        }

        // Check if another instance already exists
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Duplicate InputManager detected! Destroying this instance: " + gameObject.name);
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        
        playerInput = new PlayerInput();
        onFoot = playerInput.OnFoot;
        movement = GetComponent<PlayerMovement>();
        look = GetComponent<PlayerLook>();
        weaponManager = GetComponent<WeaponManager>();
        
        onFoot.Jump.performed += ctx => movement.Jump();
        
        // Add Sprint event handling
        onFoot.Sprint.performed += ctx => movement.Sprint(true);
        onFoot.Sprint.canceled += ctx => movement.Sprint(false);
        
        // Add callbacks for picking up and dropping weapons if WeaponManager exists
        if (weaponManager != null)
        {
            // You can add custom input actions in InputSystem if needed
            // Or use default keys via Update()
        }
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
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
            // Weapon pickup/drop is handled in WeaponManager.Update()
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
