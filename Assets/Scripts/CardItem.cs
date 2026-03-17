using UnityEngine;

public class CardItem : MonoBehaviour
{
    // A lock to prevent instant pickup
    private bool canBeCollected = false; 

    void Start()
    {
        // "Invoke" is Unity's way of saying: "Wait exactly 3 seconds, then run the EnablePickup method below"
        Invoke("EnablePickup", 3f); 
    }

    void EnablePickup()
    {
        // After 3 seconds, unlock the card!
        canBeCollected = true;
    }

    // We use "Stay" instead of "Enter" so it works even if the player is just standing under it waiting
    private void OnTriggerStay2D(Collider2D collision)
    {
        // 1. Check if it's the Player AND if the 3-second lock is open
        if (collision.CompareTag("Player") && canBeCollected == true)
        {
            PlayerInventory inventory = collision.GetComponent<PlayerInventory>();

            if (inventory != null)
            {
                inventory.AddCard(); 
                Destroy(gameObject); 
            }
        }
    }
}