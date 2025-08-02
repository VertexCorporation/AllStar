using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class TimeScene : MonoBehaviour
{
    public float delay = 24f;

    private float timer = 0f;
    private bool sceneLoaded = false;
    public AudioSource audioSource;

    public void Start()
    {
        audioSource = Object.FindFirstObjectByType<AudioSource>();
        audioSource.Pause();
    }
    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= delay && !sceneLoaded)
        {
            sceneLoaded = true;
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene("Main");
    }
}
