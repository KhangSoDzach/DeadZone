using UnityEngine;

public class VillaDoorPress : MonoBehaviour
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
        {
            GameObject ui = GameObject.FindGameObjectWithTag("PressE");
            if (ui != null)
                Instruction = ui;
        }

        if (Instruction != null)
            Instruction.SetActive(false);

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (isOpen && AnimeObject != null && AnimeObject.GetComponent<Animator>() != null)
        {
            AnimeObject.GetComponent<Animator>().Play("VillaDoorOpening");
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player") && Instruction != null)
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

            Animator animator = AnimeObject?.GetComponent<Animator>();
            if (animator != null)
            {
                if (!isOpen)
                {
                    animator.Play("VillaDoorOpening");
                    if (DoorSound != null) DoorSound.Play();
                    isOpen = true;
                }
                else
                {
                    animator.Play("VillaDoorClosing");
                    isOpen = false;
                }
            }
        }
    }
}
