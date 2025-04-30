using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public int weaponIndex;     // Index in the weaponPrefabs array
    public string weaponName;   // Name of the weapon
    public int remainingAmmo;   // Store the remaining ammo when dropped
    public bool isPistol;       // Is this a pistol (primary weapon that can't be dropped)
    
    void Start()
    {
        // Kiểm tra vị trí của vũ khí để quyết định chế độ
        bool isChildOfWeaponHolder = IsChildOfWeaponHolder();
        
        if (!isInitialized) {
            // Nếu là con của weapon holder, thì đây là vũ khí đang được trang bị
            SetPickupMode(!isChildOfWeaponHolder);
            isInitialized = true;
        }

        // Kiểm tra nếu đây là vũ khí mới nhặt, lấy vị trí đã lưu (nếu có)
        if (isPickupMode && !string.IsNullOrEmpty(weaponName))
        {
            if (lastDroppedPositions.TryGetValue(weaponName, out Vector3 savedPos))
            {
                // Chỉ sử dụng vị trí đã lưu nếu vị trí hiện tại gần đó
                // Điều này tránh trường hợp lấy vị trí của vũ khí trùng tên ở xa
                if (Vector3.Distance(transform.position, savedPos) < 5f)
                {
                    originalPosition = savedPos;
                    Debug.Log($"Vũ khí {weaponName} đang sử dụng vị trí đã lưu: {savedPos}");
                }
            }
        }
    }
    
    // Kiểm tra xem vũ khí có nằm trong weapon holder không
    bool IsChildOfWeaponHolder()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            // Kiểm tra nếu parent có tên chứa "weapon" hoặc "holder"
            if (current.name.ToLower().Contains("weapon") || 
                current.name.ToLower().Contains("holder") ||
                current.GetComponent<SwitchWeapon>() != null)
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }
    
    void OnDisable()
    {
        StopAllCoroutines();
    }
    
    // Chuyển đổi giữa trạng thái nhặt (Pickup) và trạng thái trang bị (Equipped)
    public void SetPickupMode(bool isPickup)
    {
        isPickupMode = isPickup;
        
        if (isPickup)
        {
            // Kích hoạt chế độ nhặt súng
            if (rb != null) 
            {
                rb.isKinematic = false;
                rb.useGravity = true; // Đảm bảo súng bị ảnh hưởng bởi trọng lực
            }
            
            if (pickupCollider != null) pickupCollider.enabled = true;
            
            // Tắt Gun component nếu có
            if (gunComponent != null) gunComponent.enabled = false;
            
            // Không tự động bắt đầu hiệu ứng nổi ở đây nữa
            // Hiệu ứng nổi sẽ được bắt đầu sau khi súng rơi xuống đất từ ApplyDropForce
            
            // Chỉ bắt đầu hiệu ứng nổi nếu không phải là súng vừa được vứt xuống
            if (gameObject.activeInHierarchy && (rb == null || rb.velocity.sqrMagnitude < 0.1f))
            {
                StopAllCoroutines();
                originalPosition = transform.position; // Lưu vị trí hiện tại
                StartCoroutine(FloatAndRotateEffect());
            }
        }
        else
        {
            // Kích hoạt chế độ trang bị
            StopAllCoroutines();
            
            if (rb != null) rb.isKinematic = true;
            if (pickupCollider != null) pickupCollider.enabled = false;
            
            // Kích hoạt Gun component nếu có
            if (gunComponent != null) gunComponent.enabled = true;
        }
    }
    
    // Áp dụng lực đẩy khi vũ khí được vứt ra
    public void ApplyDropForce(Vector3 direction, float force)
    {
        if (rb != null && isPickupMode)
        {
            // Đảm bảo vật lý hoạt động đúng trước khi áp dụng lực
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero; // Reset velocity
            rb.angularVelocity = Vector3.zero; // Reset angular velocity
            
            // Thêm lực đẩy có kiểm soát hơn
            rb.AddForce(direction * force * 0.5f + Vector3.down * 0.5f, ForceMode.Impulse);
            
            // Thêm một lực xoay nhẹ ngẫu nhiên
            rb.AddTorque(new Vector3(
                Random.Range(-1f, 1f), 
                Random.Range(-1f, 1f), 
                Random.Range(-1f, 1f)
            ) * force * 0.1f, ForceMode.Impulse);
            
            // Thay vì bắt đầu hiệu ứng nổi ngay lập tức, chờ một khoảng thời gian cho súng rơi xuống đất
            StartCoroutine(DelayedFloatEffect());
        }
    }
    
    // Chờ súng rơi xuống đất trước khi bắt đầu hiệu ứng nổi
    private IEnumerator DelayedFloatEffect()
    {
        // Chờ lâu hơn để đảm bảo súng đã rơi xuống đất và ổn định
        float waitTime = 3f;
        float elapsedTime = 0;
        
        // Liên tục kiểm tra khi nào vũ khí ổn định trên mặt đất
        while (elapsedTime < waitTime)
        {
            elapsedTime += 0.1f;
            
            // Kiểm tra xem súng có còn tồn tại và đang di chuyển không
            if (rb == null || !gameObject || !isPickupMode)
                yield break;
                
            // Kiểm tra nếu vũ khí đã đứng yên
            if (rb.velocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
            {
                // Thêm thời gian đệm
                yield return new WaitForSeconds(0.5f);
                break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        // Kiểm tra xem súng có còn tồn tại và đang ở chế độ pickup không
        if (gameObject != null && isPickupMode)
        {
            // Lưu vị trí mới sau khi rơi xuống đất
            originalPosition = transform.position;
            
            // Lưu vị trí vào dictionary để sử dụng lại khi nhặt vũ khí trùng tên
            if (!string.IsNullOrEmpty(weaponName))
            {
                lastDroppedPositions[weaponName] = originalPosition;
                Debug.Log($"Đã lưu vị trí cho vũ khí {weaponName}: {originalPosition}");
            }
            
            // Bắt đầu hiệu ứng nổi và xoay
            StartCoroutine(FloatAndRotateEffect());
        }
    }
    
    // Hiệu ứng nổi và xoay cho vũ khí có thể nhặt
    IEnumerator FloatAndRotateEffect()
    {
        float time = 0;
        
        while (true)
        {
            time += Time.deltaTime;
            
            // Hiệu ứng xoay
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            
            // Hiệu ứng nổi lên xuống
            float newY = originalPosition.y + Mathf.Sin(time * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            
            yield return null;
        }
    }
}