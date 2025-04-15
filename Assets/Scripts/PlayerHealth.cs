using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Player Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI Elements")]
    public Image healthBarImage; // Image để hiển thị lượng máu
    public Image healthBorderImage; // Image viền (màu đen)

    [Header("Health Colors")]
    public Color fullHealthColor = Color.green; // Màu xanh lá khi đầy máu
    public Color lowHealthColor = Color.red; // Màu đỏ khi gần chết

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI(); // Cập nhật thanh máu

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthUI()
    {
        if (healthBarImage != null)
        {
            healthBarImage.fillAmount = currentHealth / maxHealth;
            healthBarImage.color = Color.Lerp(lowHealthColor, fullHealthColor, currentHealth / maxHealth);
        }
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        // Thêm logic chết tại đây (ví dụ: load lại scene, hiển thị màn hình game over, v.v.)
    }
}
