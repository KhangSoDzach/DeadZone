using UnityEngine;
using System.Collections;

public class PressButtonToOpen : MonoBehaviour
{
    public GameObject Instruction;
    public GameObject AnimeObject;
    public GameObject TheTrigger;
    public AudioSource DoorSound;
    public float interactDistance = 3f;
    public Transform player;
    public bool isOpen = false;

    private bool playerInZone = false;
    private bool isWaitingForPlayer = false;

    void Start()
    {
        if (Instruction == null)
        {
            GameObject ui = GameObject.FindGameObjectWithTag("PressE");
            if (ui != null)
                Instruction = ui;
        }

        if (Instruction != null)
            Instruction.SetActive(false);

        if (!isOpen && AnimeObject != null)
        {
            Animator anim = AnimeObject.GetComponent<Animator>();
            if (anim != null)
            {
                anim.Play("DoorOpening");
                isOpen = true;
            }
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

       private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            if (Instruction != null)
                Instruction.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            if (Instruction != null)
                Instruction.SetActive(false);
        }
    }

    void Update()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                return;
        }

        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (Instruction != null)
            Instruction.SetActive(distance <= interactDistance);

        if (Input.GetKeyDown(KeyCode.E) && distance <= interactDistance)
        {
            if (Instruction != null)
                Instruction.SetActive(false);

            Animator anim = AnimeObject?.GetComponent<Animator>();
            if (anim != null)
            {
                if (!isOpen)
                {
                    anim.Play("DoorOpening");
                    DoorSound?.Play();
                }
                else
                {
                    anim.Play("DoorClosing");
                }
                isOpen = !isOpen;
            }
        }
    }
}
