using UnityEngine;

public class BrickDoorPress : MonoBehaviour
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
        if (player == null) return;
        float distance = Vector3.Distance(transform.position, player.position);

        if (Input.GetKeyDown(KeyCode.E) && distance <= interactDistance)
        {
            if (Instruction != null)
                Instruction.SetActive(false);

            Animator anim = AnimeObject?.GetComponent<Animator>();
            if (anim != null)
            {
                if (!isOpen)
                {
                    anim.Play("BrickDoorOpening");
                    if (DoorSound != null) DoorSound.Play();
                }
                else
                {
                    anim.Play("BrickDoorClosing");
                }

                isOpen = !isOpen;
            }
        }
    }
}
