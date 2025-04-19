using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Zombie_1 : MonoBehaviour
{   

    // Start is called before the first frame update
    void Start()
    {
        
    }

    [Header("Zombie Things")]
    public LayerMask PlayerLayer;
    public NavMeshAgent zombieAgent;
    public Transform LookPoint;
    public Transform playerBody;
    public Camera AttackingRaycastArea;

    [Header("Zombie Behavior")]
    public GameObject[] guardingPoints;
    public int currentPosition = 0;
    public float zombieSpeed;
    public float walkingRadius = 2;

    [Header("Zombie Attacking Zone")]
    public float attackSpeed;
    public bool prevAttack;

    [Header("Zombie States")]
    public float observationRadius;
    public float attackingRadius;
    public bool playerExistenceRadius;
    public bool playerInAttackingRadius;

    [Header("Zombie Health")]
    public float attackDamage = 5f;


    // Update is called once per frame
    void Update()
    {
        playerExistenceRadius = Physics.CheckSphere(transform.position, observationRadius, PlayerLayer);
        playerInAttackingRadius = Physics.CheckSphere(transform.position, attackingRadius, PlayerLayer);
        if (!playerExistenceRadius && !playerInAttackingRadius) Guard();
        if (playerExistenceRadius && !playerInAttackingRadius) ChasingPlayer();
        if (playerExistenceRadius && playerInAttackingRadius) AttackPlayer();
    }
    private void Awake()
    {
        zombieAgent = GetComponent<NavMeshAgent>();

    }
    private void Guard()
    {
        if (Vector3.Distance(guardingPoints[currentPosition].transform.position,transform.position)<walkingRadius)
        {
            currentPosition = Random.Range(0, guardingPoints.Length);
            if (currentPosition >= guardingPoints.Length)
            {
                currentPosition = 0;
            }
        }
        transform.position = Vector3.MoveTowards(transform.position, guardingPoints[currentPosition].transform.position,Time.deltaTime *zombieSpeed);

        //change zombie facing
        transform.LookAt(guardingPoints[currentPosition].transform.position);
    }
    private void ChasingPlayer()
    {
        zombieAgent.SetDestination(playerBody.position);
    }
    private void AttackPlayer()
    {
        zombieAgent.SetDestination(transform.position);
        transform.LookAt(LookPoint);
        if(!prevAttack)
        {
            //Debug.Log("Zombie trying to attack player. Distance to player: " + Vector3.Distance(transform.position, playerBody.position));
            
            // Kiểm tra trực tiếp từ playerBody (cách đáng tin cậy nhất)
            HealthManager playerHealth = null;
            
            if (playerBody != null)
            {
                // Kiểm tra component trên player trước
                playerHealth = playerBody.GetComponent<HealthManager>();
                
                // Nếu không tìm thấy, kiểm tra trong các thành phần con
                if (playerHealth == null)
                {
                    playerHealth = playerBody.GetComponentInChildren<HealthManager>();
                    
                    if (playerHealth != null)
                    {
                        //Debug.Log("Found HealthManager in player's child object");
                    }
                }
                
                // Nếu vẫn không tìm thấy, kiểm tra trong cha của đối tượng
                if (playerHealth == null && playerBody.parent != null)
                {
                    playerHealth = playerBody.parent.GetComponent<HealthManager>();
                    
                    if (playerHealth != null)
                    {
                        //Debug.Log("Found HealthManager in player's parent object");
                    }
                }
                
                // Nếu vẫn không tìm thấy, thử tìm trong toàn bộ scene với tag "Player"
                if (playerHealth == null)
                {
                    GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                    if (playerObject != null && playerObject != playerBody.gameObject)
                    {
                        playerHealth = playerObject.GetComponent<HealthManager>();
                        
                        if (playerHealth != null)
                        {
                           // Debug.Log("Found HealthManager through Player tag");
                        }
                    }
                }
            }
            
            // Phương pháp dự phòng: Raycast
            if (playerHealth == null && AttackingRaycastArea != null)
            {
                RaycastHit hit;
                if (Physics.Raycast(AttackingRaycastArea.transform.position, AttackingRaycastArea.transform.forward, out hit, attackingRadius))
                {
                    Debug.Log("Raycast hit: " + hit.transform.name);
                    playerHealth = hit.transform.GetComponent<HealthManager>();
                    
                    if (playerHealth == null && hit.transform.parent != null)
                    {
                        playerHealth = hit.transform.parent.GetComponent<HealthManager>();
                    }
                }
            }
            
            // Gây sát thương nếu tìm thấy HealthManager
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
               // Debug.Log("Player took damage: " + attackDamage);
            }
            else
            {
               // Debug.LogWarning("No HealthManager found on player. Make sure the Player has the HealthManager component attached!");
                
                // Hiển thị thông tin chi tiết hơn để gỡ lỗi
                if (playerBody == null)
                {
                    Debug.LogError("playerBody reference is null!");
                }
                else
                {
                    Debug.LogWarning("Player object name: " + playerBody.name + ", Has tag 'Player': " + playerBody.CompareTag("Player"));
                    MonoBehaviour[] components = playerBody.GetComponents<MonoBehaviour>();
                    Debug.LogWarning("Player has " + components.Length + " MonoBehaviour components");
                    foreach (MonoBehaviour comp in components)
                    {
                        Debug.LogWarning("- " + comp.GetType().Name);
                    }
                }
            }
            
            prevAttack = true;
            Invoke(nameof(ActiveAttacking), attackSpeed);
        }
    }
    private void ActiveAttacking()
    {
        prevAttack = false;
    }
}
