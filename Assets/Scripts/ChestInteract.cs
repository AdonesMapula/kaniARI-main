using UnityEngine;

public class ChestInteract : MonoBehaviour
{
    private Animator anim;
    private bool isOpen = false; 

    [Header("Loot Settings")]
    public GameObject cardPrefab; // The item to spawn
    public Transform spawnPoint;  // Where it spawns from

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // The 'isOpen == false' is what permanently locks it after one touch!
        if (collision.CompareTag("Player") && isOpen == false)
        {
            isOpen = true; 
            anim.SetTrigger("Open"); 

            // Magically spawn the Card Wabalo at the spawn point!
            if (cardPrefab != null && spawnPoint != null)
            {
                Instantiate(cardPrefab, spawnPoint.position, Quaternion.identity);
            }
        }
    }
}