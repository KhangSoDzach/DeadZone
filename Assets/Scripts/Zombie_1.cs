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
            RaycastHit hit;
            Debug.DrawRay(AttackingRaycastArea.transform.position, AttackingRaycastArea.transform.forward * attackingRadius, Color.red, 1f); // Vẽ raycast để kiểm tra
            if (Physics.Raycast(AttackingRaycastArea.transform.position,AttackingRaycastArea.transform.forward ,out hit, attackingRadius))
            {
                Debug.Log($"Raycast hit: {hit.transform.name}"); // Thêm thông báo gỡ lỗi
                if (hit.transform.CompareTag("Player"))
                {
                    Debug.Log("Zombie is attacking Player!");
                    PlayerHealth playerHealth = hit.transform.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(attackDamage); // Gây sát thương lên Player
                    }
                }
                else
                {
                    Debug.Log("Raycast did not hit the Player.");
                }
            }
            else
            {
                Debug.Log("Raycast did not hit anything.");
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
