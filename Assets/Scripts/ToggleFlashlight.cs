using UnityEngine;

public class ToggleFlashlight : MonoBehaviour
{
    public Light flashlight;  

    void Start()
    {
        if (flashlight == null)
        {
            flashlight = GetComponent<Light>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (flashlight != null)
                flashlight.enabled = !flashlight.enabled;
        }
    }
}
