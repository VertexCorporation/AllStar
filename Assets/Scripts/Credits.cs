using UnityEngine;

public class Credits : MonoBehaviour
{
    public float speed = 50f;

    private void Start()
    {
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.up * speed;
    }
}
