using UnityEngine;

public class PlayerAn : MonoBehaviour
{
    public GameObject[] ps;

    public void Death()
    {
        Instantiate(ps[0], transform.position, Quaternion.identity);
    }
}
