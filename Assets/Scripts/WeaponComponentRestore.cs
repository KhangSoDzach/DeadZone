using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Helper script to restore missing components on weapons after pickup
public class WeaponComponentRestore : MonoBehaviour
{
    // Thêm biến để lưu trữ vị trí ban đầu
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private bool hasStoredTransform = false;

    private void Start()
    {
        // Kiểm tra xem có đang ở trong scene gameplay không
        if (!IsGameplayScene())
        {
            Debug.Log($"WeaponComponentRestore: Không áp dụng trong scene này: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            return;
        }

        // Lấy WeaponManager để áp dụng transform chính nếu có
        WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager != null)
        {
            // Thêm null check cho method ApplyPrimaryTransform nếu tồn tại
            try
            {
                var method = weaponManager.GetType().GetMethod("ApplyPrimaryTransform");
                if (method != null)
                {
                    method.Invoke(weaponManager, new object[] { this });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not apply primary transform: {e.Message}");
            }
        }

        // Lưu vị trí ban đầu của súng ngay khi bắt đầu game
        if (!hasStoredTransform)
        {
            StoreCurrentTransformAsCorrect();
        }

        // Run a delayed check to restore components after everything is initialized
        StartCoroutine(DelayedComponentCheck());
    }
    
    // Kiểm tra xem có đang ở scene gameplay không
    private bool IsGameplayScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        return !sceneName.Contains("menu") && !sceneName.Contains("login") && !sceneName.Contains("main");
    }
    
    private IEnumerator DelayedComponentCheck()
    {
        // Wait a moment for everything else to initialize
        yield return new WaitForSeconds(0.5f);
        
        // Chỉ tiếp tục nếu đang ở scene gameplay
        if (!IsGameplayScene())
        {
            yield break;
        }
        
        // Find all Gun components
        Gun[] allGuns = FindObjectsOfType<Gun>(true); // Include inactive weapons
        
        foreach (Gun gun in allGuns)
        {
            if (gun != null)
            {
                RestoreGunComponents(gun);
            }
        }
        
        Debug.Log($"WeaponComponentRestore: Đã kiểm tra {allGuns.Length} vũ khí");
    }
    
    // Called when weapon is picked up or created
    public void RestoreGunComponents(Gun gun)
    {
        if (gun == null) return;
        
        // Check for missing components
        bool componentsFixed = false;
        
        // 1. Check Animator
        if (gun.animator == null)
        {
            gun.animator = gun.GetComponent<Animator>();
            if (gun.animator == null)
                gun.animator = gun.GetComponentInChildren<Animator>();
            if (gun.animator == null)
                gun.animator = gun.gameObject.AddComponent<Animator>();
                
            componentsFixed = true;
            Debug.Log($"Restored animator on {gun.name}");
        }
        
        // 2. Check AudioSource
        if (gun.gunshotSound == null)
        {
            gun.gunshotSound = gun.GetComponent<AudioSource>();
            if (gun.gunshotSound == null)
                gun.gunshotSound = gun.GetComponentInChildren<AudioSource>();
            if (gun.gunshotSound == null)
            {
                AudioSource newAudio = gun.gameObject.AddComponent<AudioSource>();
                newAudio.playOnAwake = false;
                gun.gunshotSound = newAudio;
            }
            
            componentsFixed = true;
            Debug.Log($"Restored AudioSource on {gun.name}");
        }
        
        // 3. Check impactEffect
        if (gun.impactEffect == null)
        {
            // Try to find a default impact effect in the Resources folder
            GameObject defaultImpact = Resources.Load<GameObject>("DefaultImpact");
            if (defaultImpact != null)
            {
                gun.impactEffect = defaultImpact;
                componentsFixed = true;
                Debug.Log($"Restored impactEffect on {gun.name}");
            }
        }
        
        // 4. Check UI references
        if (gun.ammoText == null)
        {
            // Find UI components by common naming patterns
            TextMeshProUGUI[] texts = Object.FindObjectsOfType<TextMeshProUGUI>();
            foreach (TextMeshProUGUI text in texts)
            {
                if (text.name.ToLower().Contains("ammo"))
                {
                    gun.ammoText = text;
                    componentsFixed = true;
                    Debug.Log($"Found ammoText: {text.name}");
                    break;
                }
            }
        }
        
        // 5. Check for ScoreManager and create if needed
        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager == null)
        {
            GameObject scoreManagerObject = new GameObject("ScoreManager");
            scoreManager = scoreManagerObject.AddComponent<ScoreManager>();
            scoreManager.FindScoreText();
            componentsFixed = true;
            Debug.Log("Created ScoreManager");
        }
        else if (scoreManager.scoreText == null)
        {
            // Find a score text for the ScoreManager
            Text[] allTexts = FindObjectsOfType<Text>();
            foreach (Text text in allTexts)
            {
                if (text.name.ToLower().Contains("score"))
                {
                    scoreManager.scoreText = text;
                    scoreManager.UpdateScoreUI();
                    componentsFixed = true;
                    Debug.Log($"Found and assigned scoreText to ScoreManager: {text.name}");
                    break;
                }
            }
        }
        
        // 6. If playerCamera is missing, use main camera
        if (gun.playerCamera == null)
        {
            gun.playerCamera = Camera.main;
            componentsFixed = true;
            Debug.Log($"Set Camera.main as playerCamera for {gun.name}");
        }
        
        // 7. Now update the UI to show the current ammo
        gun.UpdateAmmoUI();
        
        if (componentsFixed)
        {
            Debug.Log($"WeaponComponentRestore: Fixed missing components on {gun.name}");
        }
        
        // Nếu chưa lưu transform, lưu lại ngay bây giờ
        // if (!hasStoredTransform && transform.parent != null)
        // {
        //     StoreCurrentTransformAsCorrect();
        // }
    }
    
    // Lưu vị trí, góc quay và tỷ lệ hiện tại của vũ khí làm vị trí chuẩn
    public void StoreCurrentTransformAsCorrect()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;
        hasStoredTransform = true;

        Debug.Log($"WeaponComponentRestore: Đã lưu transform cho {gameObject.name}: pos={originalPosition}, rot={originalRotation.eulerAngles}, scale={originalScale}");
    }

    public void StoreCurrentTransformAsCorrect(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        originalPosition = position;
        originalRotation = rotation;
        originalScale = scale;
        hasStoredTransform = true;

        Debug.Log($"WeaponComponentRestore: Đã lưu transform từ tham số: pos={originalPosition}, rot={originalRotation.eulerAngles}, scale={originalScale}");
    }
    
    // Phương thức để chỉnh sửa thủ công vị trí đã lưu
    public void SetStoredPosition(Vector3 position)
    {
        originalPosition = position;
        hasStoredTransform = true;  // Đánh dấu là đã có giá trị được lưu
        Debug.Log($"WeaponComponentRestore: Đã chỉnh sửa vị trí lưu trữ thành: {originalPosition}");
    }

    // Phương thức để chỉnh sửa thủ công góc quay đã lưu
    public void SetStoredRotation(Quaternion rotation)
    {
        originalRotation = rotation;
        hasStoredTransform = true;  // Đánh dấu là đã có giá trị được lưu
        Debug.Log($"WeaponComponentRestore: Đã chỉnh sửa góc quay lưu trữ thành: {originalRotation.eulerAngles}");
    }

    // Phương thức để chỉnh sửa thủ công tỷ lệ đã lưu
    public void SetStoredScale(Vector3 scale)
    {
        originalScale = scale;
        hasStoredTransform = true;  // Đánh dấu là đã có giá trị được lưu
        Debug.Log($"WeaponComponentRestore: Đã chỉnh sửa tỷ lệ lưu trữ thành: {originalScale}");
    }

    // Phương thức chuẩn bị vũ khí trước khi vô hiệu hóa
    public void PrepareForDrop()
    {
        // Lưu vị trí hiện tại nếu chưa lưu
        // if (!hasStoredTransform)
        // {
        //     StoreCurrentTransformAsCorrect();
        // }
        
        // Có thể thực hiện thêm các hành động khác trước khi vô hiệu hóa vũ khí nếu cần
    }
    
    public void ResetPosition()
    {
        // Kiểm tra xem GameObject này có GunScript không
        GunScript gunScript = GetComponent<GunScript>();
        
        // Nếu có GunScript, bỏ qua việc khôi phục vị trí để tránh xung đột
        if (gunScript != null && gunScript.enabled)
        {
            Debug.Log($"WeaponComponentRestore: Không áp dụng vị trí cố định cho {gameObject.name} vì đã có GunScript.");
            return;
        }
        
        // Nếu không có GunScript, áp dụng vị trí cố định
        if (hasStoredTransform)
        {
            // Lưu lại giá trị cũ để kiểm tra sự thay đổi
            Vector3 oldPos = transform.localPosition;
            Quaternion oldRot = transform.localRotation;
            Vector3 oldScale = transform.localScale;
            
            // Áp dụng chính xác các giá trị đã lưu
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            transform.localScale = originalScale;
            
            // Log chi tiết để dễ theo dõi
            Debug.Log($"WeaponComponentRestore: Đã khôi phục transform cho {gameObject.name}:" +
                      $"\n - Position: {oldPos} -> {originalPosition}" +
                      $"\n - Rotation: {oldRot.eulerAngles} -> {originalRotation.eulerAngles}" +
                      $"\n - Scale: {oldScale} -> {originalScale}");
        }
        else
        {
            Debug.LogWarning($"WeaponComponentRestore: Không thể đặt vị trí cho {gameObject.name} vì chưa có transform được lưu trữ!");
        }
    }

    // Public method to check if the transform has been stored
    public bool HasStoredTransform()
    {
        return hasStoredTransform;
    }
}