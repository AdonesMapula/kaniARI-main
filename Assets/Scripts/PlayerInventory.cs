using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory")]
    public int cardsCollected = 0; // Starts at 0

    // The card will call this specific method when touched
    public void AddCard()
    {
        cardsCollected++; // Adds 1 to the total
        
        // Let's print a funny Bisaya message to the console so we know it worked!
        Debug.Log("Kuha na ang Card Wabalo! Total cards: " + cardsCollected);
    }
}