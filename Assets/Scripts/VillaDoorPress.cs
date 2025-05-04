using System.Collections;
using System.Collections.Generic;
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
    // Start is called before the first frame update
    void Start()
    {
        Instruction.SetActive(false);
        if (isOpen)
        {
            AnimeObject.GetComponent<Animator>().Play("VillaDoorOpening");
        }
    }
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.transform.tag == "Player")
        {
            Instruction.SetActive(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        Instruction.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (Input.GetKeyDown(KeyCode.E) && distance <= interactDistance)
        {
            if (!isOpen)
            {

                Instruction.SetActive(false);
                AnimeObject.GetComponent<Animator>().Play("VillaDoorOpening");
                DoorSound.Play();
                isOpen = true;
            }
            else
            {
                Instruction.SetActive(false);
                AnimeObject.GetComponent<Animator>().Play("VillaDoorClosing");
                isOpen = false;
            }
        }

    }
}
