using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    [Range(0f, 1f)]
    public float normalAlpha = 0f; // Mức độ trong suốt ban đầu (0 = hoàn toàn trong suốt)
    [Range(0f, 1f)]
    public float hoverAlpha = 0.8f; // Mức độ trong suốt khi hover (1 = hoàn toàn hiển thị)
    public float transitionSpeed = 5f; // Tốc độ chuyển đổi

    private Image backgroundImage; // Hình nền của nút
    private bool isHovering = false;
    private Color targetColor;

    void Start()
    {
        // Lấy component Image của nút
        backgroundImage = GetComponent<Image>();
        
        if (backgroundImage == null)
        {
            Debug.LogError("Không tìm thấy component Image trên GameObject " + gameObject.name);
            return;
        }
        
        // Lưu màu ban đầu nhưng đặt alpha về giá trị normalAlpha
        Color initialColor = backgroundImage.color;
        initialColor.a = normalAlpha;
        backgroundImage.color = initialColor;
        
        // Thiết lập màu mục tiêu ban đầu
        targetColor = initialColor;
    }

    void Update()
    {
        // Chuyển đổi màu hiện tại sang màu mục tiêu một cách mượt mà
        if (backgroundImage != null && backgroundImage.color != targetColor)
        {
            backgroundImage.color = Color.Lerp(backgroundImage.color, targetColor, Time.deltaTime * transitionSpeed);
        }
    }

    // Khi con trỏ di chuyển vào nút
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (backgroundImage != null)
        {
            // Lấy màu hiện tại và chỉ thay đổi alpha
            Color hoverColor = backgroundImage.color;
            hoverColor.a = hoverAlpha;
            targetColor = hoverColor;
        }
    }

    // Khi con trỏ di chuyển ra khỏi nút
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (backgroundImage != null)
        {
            // Lấy màu hiện tại và chỉ thay đổi alpha
            Color normalColor = backgroundImage.color;
            normalColor.a = normalAlpha;
            targetColor = normalColor;
        }
    }
}
