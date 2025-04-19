using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Script để kiểm tra hiệu ứng bị tấn công bằng phím tắt
public class DamageEffectTester : MonoBehaviour
{
    private PlayerLook playerLook;
    private HealthManager healthManager;

    void Start()
    {
        // Tìm các component cần thiết
        playerLook = FindObjectOfType<PlayerLook>();
        healthManager = FindObjectOfType<HealthManager>();

        if (playerLook == null)
            Debug.LogError("Không tìm thấy PlayerLook trong scene!");
        else
            Debug.Log("Đã tìm thấy PlayerLook: " + playerLook.gameObject.name);

        if (healthManager == null)
            Debug.LogError("Không tìm thấy HealthManager trong scene!");
        else
            Debug.Log("Đã tìm thấy HealthManager: " + healthManager.gameObject.name);
    }

    void Update()
    {
        // Phím F để kích hoạt hiệu ứng rung camera và viền đỏ trực tiếp
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("Phím F được nhấn - Kích hoạt hiệu ứng viền đỏ trực tiếp");
            if (playerLook != null)
                playerLook.TakeDamageEffect();
        }

        // Phím G để kích hoạt hiệu ứng thông qua HealthManager
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("Phím G được nhấn - Kích hoạt hiệu ứng thông qua HealthManager");
            if (healthManager != null)
                healthManager.TakeDamage(10f);
        }

        // Phím H để kiểm tra xem damageVignette có tồn tại không
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (playerLook != null)
            {
                string status = "Trạng thái hiệu ứng viền đỏ:\n";
                
                // Phản chiếu để truy cập các biến riêng tư
                var field = typeof(PlayerLook).GetField("damageVignette", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.NonPublic);
                
                var vignette = field?.GetValue(playerLook) as UnityEngine.UI.Image;
                status += "damageVignette: " + (vignette != null ? "Tồn tại" : "Không tồn tại") + "\n";
                
                field = typeof(PlayerLook).GetField("damageCanvas", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.NonPublic);
                
                var canvas = field?.GetValue(playerLook) as GameObject;
                status += "damageCanvas: " + (canvas != null ? "Tồn tại" : "Không tồn tại") + "\n";
                
                if (canvas != null)
                {
                    status += "Canvas active: " + canvas.activeInHierarchy + "\n";
                    var canvasComponent = canvas.GetComponent<Canvas>();
                    if (canvasComponent != null)
                    {
                        status += "Canvas sorting order: " + canvasComponent.sortingOrder;
                    }
                }
                
                Debug.Log(status);
            }
        }

        // Phím J để tạo lại hiệu ứng viền đỏ
        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("Phím J được nhấn - Tạo lại hiệu ứng viền đỏ");
            if (playerLook != null)
            {
                // Gọi phương thức CreateDamageVignetteEffect bằng reflection
                var method = typeof(PlayerLook).GetMethod("CreateDamageVignetteEffect", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.NonPublic);
                
                method?.Invoke(playerLook, null);
            }
        }
    }
}