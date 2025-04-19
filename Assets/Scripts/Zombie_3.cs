using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Zombie_3 : MonoBehaviour
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

    [Header("Zombie Animations")]
    public Animator aniZombie;

    [Header("Zombie States")]
    public float observationRadius;
    public float attackingRadius;
    public bool playerExistenceRadius;
    public bool playerInAttackingRadius;

    [Header("Zombie Health and Damage")]
    public float attackDamage = 5f;
    private float zombieHealth = 100f;
    private float remainHeath;


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
        remainHeath = zombieHealth;
        zombieAgent = GetComponent<NavMeshAgent>();

    }
    private void Guard()
    {
        if (Vector3.Distance(guardingPoints[currentPosition].transform.position, transform.position) < walkingRadius)
        {
            currentPosition = Random.Range(0, guardingPoints.Length);
            if (currentPosition >= guardingPoints.Length)
            {
                currentPosition = 0;
            }
        }
        transform.position = Vector3.MoveTowards(transform.position, guardingPoints[currentPosition].transform.position, Time.deltaTime * zombieSpeed);

        //change zombie facing
        transform.LookAt(guardingPoints[currentPosition].transform.position);
    }
    private void ChasingPlayer()
    {
        if (zombieAgent.SetDestination(playerBody.position))
        {
            zombieAgent.speed = 1;
            aniZombie.SetBool("isWalking", true);
            aniZombie.SetBool("isRunning", false);
            aniZombie.SetBool("isAttacking", false);
            aniZombie.SetBool("isDead", false);

        }
        else
        {
            aniZombie.SetBool("isWalking", false);
            aniZombie.SetBool("isRunning", false);
            aniZombie.SetBool("isAttacking", false);
            aniZombie.SetBool("isDead", true);
        }
    }
    private void AttackPlayer()
    {
        zombieAgent.SetDestination(transform.position);
        transform.LookAt(LookPoint);
        if (!prevAttack)
        {
            RaycastHit hit;
            if (Physics.Raycast(AttackingRaycastArea.transform.position, AttackingRaycastArea.transform.forward, out hit, attackingRadius))
            {
                //Code the player take damage from player

                aniZombie.SetBool("isAttacking", true);
                aniZombie.SetBool("isWalking", false);
                aniZombie.SetBool("isRunning", false);
                aniZombie.SetBool("isDead", false);

                Debug.Log("Attack" + hit.transform.name);
                
                // Apply damage to the player
                HealthManager healthManager = hit.transform.GetComponent<HealthManager>();
                if (healthManager == null)
                {
                    healthManager = hit.transform.GetComponentInParent<HealthManager>();
                }
                
                if (healthManager != null)
                {
                    healthManager.TakeDamage(attackDamage);
                    Debug.Log("Player damaged by zombie: " + attackDamage);
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
    public void zombieGotHit(float takeDamge)
    {
        remainHeath -= takeDamge;
        if (remainHeath <= 0)
        {
            zombieDie();

            aniZombie.SetBool("isWalking", false);
            aniZombie.SetBool("isRunning", false);
            aniZombie.SetBool("isAttacking", false);
            aniZombie.SetBool("isDead", true);
        }
    }

    private void zombieDie()
    {
        transform.LookAt(LookPoint);
        zombieAgent.SetDestination(transform.position);
        zombieSpeed = 0;
        attackingRadius = 0;
        observationRadius = 0;
        playerInAttackingRadius = false;
        playerExistenceRadius = false;

        aniZombie.SetBool("isWalking", false);
        aniZombie.SetBool("isRunning", false);
        aniZombie.SetBool("isAttacking", false);
        aniZombie.SetBool("isDead", true);
        Object.Destroy(gameObject, 5.0f);
    }
}
