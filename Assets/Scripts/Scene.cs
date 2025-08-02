using UnityEngine;
using UnityEngine.SceneManagement;

public class Scene : MonoBehaviour
{
    public void OtherScene(string SceneName)
    {
        SceneManager.LoadScene(SceneName);
    }
}
