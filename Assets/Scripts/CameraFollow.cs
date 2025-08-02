using System;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject target;
    public float p = 1.02f;
    float initialDeltaY;
    public Impact impact;
    public Skill s;
    public GameManager gm;

    void Start()
    {
        initialDeltaY = target.transform.position.y - transform.position.y;
    }

    void Update()
    {
        float deltaYP = p - transform.position.y;
        float deltaY = target.transform.position.y - transform.position.y;
        p = target.transform.position.y - 2.7f;
        if (s.py == true)
        {
            if (initialDeltaY > deltaYP && gm.GameState == GameState.Playing)
            {
                transform.position = transform.position + new Vector3(0, -0.02f, 0);
                Debug.Log(initialDeltaY);
                Debug.Log(deltaYP);
            }
            if (initialDeltaY < deltaYP + 0.1 && gm.GameState == GameState.Playing)
            {
                transform.position = transform.position + new Vector3(0, 0.02f, 0);
            }
        }
        else
        {
            if (initialDeltaY > deltaY && gm.GameState == GameState.Playing)
            {
                transform.position = transform.position + new Vector3(0, -0.02f, 0);
            }
            if (initialDeltaY < deltaY + 0.1f && gm.GameState == GameState.Playing)
            {
                transform.position = transform.position + new Vector3(0, 0.02f, 0);
            }
        }
    }
}