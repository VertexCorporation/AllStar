using UnityEngine;

public class VertexWobble : MonoBehaviour
{
    public float strength = 20f;

    private Vector3 originalPosition;

    private void Start()
    {
        originalPosition = transform.position;
    }

    private void Update()
    {
        Vector2 randomOffset = Random.insideUnitCircle * strength;
        transform.position = originalPosition + (Vector3)randomOffset;
    }
}