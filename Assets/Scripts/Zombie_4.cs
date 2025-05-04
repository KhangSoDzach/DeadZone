using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Zombie_4 : MonoBehaviour
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

    [Header("Zombie Standing")]
    public float zombieSpeed;

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
    public float attackDamage = 3f;
    private float zombieHealth = 100f;
    private float remainHeath;

    [Header("Zombie Sounds")]
    public AudioSource audioSource;
    public AudioClip idleGroanSound;

    private float nextSoundTime = 0f;

    // Update is called once per frame
    void Update()
    {
        playerExistenceRadius = Physics.CheckSphere(transform.position, observationRadius, PlayerLayer);
        playerInAttackingRadius = Physics.CheckSphere(transform.position, attackingRadius, PlayerLayer);
        if (playerExistenceRadius && !playerInAttackingRadius) ChasingPlayer();
        if (playerExistenceRadius && playerInAttackingRadius) AttackPlayer();

        if (playerExistenceRadius && !playerInAttackingRadius)
        {
            if (Time.time >= nextSoundTime)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.PlayOneShot(idleGroanSound);
                }

                nextSoundTime = Time.time + 5f;
            }
        }
        if (!playerExistenceRadius && !playerInAttackingRadius)
        {
            Idle();
        }
    }
    private void Awake()
    {
        remainHeath = zombieHealth;
        zombieAgent = GetComponent<NavMeshAgent>();

    }
    private void Idle()
    {
        zombieAgent.SetDestination(transform.position);
        aniZombie.SetBool("isIdle", true);
        aniZombie.SetBool("isRunning", false);
    }
    private void ChasingPlayer()
    {
        if (zombieAgent.SetDestination(playerBody.position))
        {
            zombieAgent.speed = 5.5f;
            aniZombie.SetBool("isIdle", false);
            aniZombie.SetBool("isRunning", true);
            aniZombie.SetBool("isAttacking", false);
        }

    }
    private void AttackPlayer()
    {
        zombieAgent.SetDestination(transform.position);
        transform.LookAt(LookPoint);
        if (!prevAttack)
        {
            ApplyZombieDamage();
            aniZombie.SetBool("isRunning", false);
            aniZombie.SetBool("isAttacking", true);


            Invoke(nameof(ApplyZombieDamage), 0.3f);
            prevAttack = true;
            Invoke(nameof(ActiveAttacking), attackSpeed);
        }
    }
    private void ApplyZombieDamage()
    {
        RaycastHit hit;
        if (Physics.Raycast(AttackingRaycastArea.transform.position, AttackingRaycastArea.transform.forward, out hit, attackingRadius))
        {
            // Check if we hit the player
            HealthManager playerHealth = hit.transform.GetComponent<HealthManager>();
            if (playerHealth != null)
            {
                // Apply damage to the player
                playerHealth.TakeDamage(attackDamage);
                Debug.Log("Player hit for " + attackDamage + " damage");
            }
        }
    }
    private void ActiveAttacking()
    {
        prevAttack = false;
    }
    public void zombieGotHit(float takeDamge)
    {
        observationRadius = 30f;
        remainHeath -= takeDamge;
        if (remainHeath <= 0)
        {
            zombieDie();

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

        aniZombie.SetBool("isDead", true);
        Object.Destroy(gameObject, 5.0f);
    }
}
