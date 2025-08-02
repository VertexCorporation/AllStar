using UnityEngine;

public class Bounce : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D coll)
    {
        if (_animator != null && coll.CompareTag("Player"))
        {
            _animator.SetTrigger("StampedOn");
        }
    }
}
