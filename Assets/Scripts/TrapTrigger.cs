using UnityEngine;

public class TrapTrigger : MonoBehaviour
{
    public Rigidbody2D trap;
    public float dropGravity = 1.5f;
    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        // Only trigger once
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;

            // Activate physics for the trap
            trap.bodyType = RigidbodyType2D.Dynamic;
            trap.gravityScale = dropGravity;
        }
    }
}