using UnityEngine;

public class BlackDoorPress : MonoBehaviour
{
    public GameObject Instruction;
    public GameObject AnimeObject;
    public GameObject TheTrigger;
    public AudioSource DoorSound;
    public float interactDistance = 3f;
    public Transform player;
    public bool isOpen = false;

    void Start()
    {
        if (Instruction == null)
            Instruction = GameObject.FindGameObjectWithTag("PressE");

        if (Instruction != null)
            Instruction.SetActive(false);

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && Instruction != null)
        {
            Instruction.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && Instruction != null)
        {
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

        float distance = Vector3.Distance(transform.position, player.position);

        if (Input.GetKeyDown(KeyCode.E) && distance <= interactDistance)
        {
            if (Instruction != null)
                Instruction.SetActive(false);

            if (AnimeObject != null && AnimeObject.GetComponent<Animator>() != null)
            {
                Animator animator = AnimeObject.GetComponent<Animator>();

                if (!isOpen)
                {
                    animator.Play("BlackDoorOpening");
                    if (DoorSound != null) DoorSound.Play();
                }
                else
                {
                    animator.Play("BlackDoorClosing");
                }

                isOpen = !isOpen;
            }
        }
    }
}
