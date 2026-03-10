using UnityEngine;

public class dropPlatformer : MonoBehaviour
{
    public Rigidbody2D trap;

    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            trap.gravityScale = 5f;
        }
    }
}