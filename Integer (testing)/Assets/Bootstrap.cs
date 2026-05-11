using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    void Start()
    {
        if (!SceneManager.GetSceneByName("Menu").isLoaded)
        {
            SceneManager.LoadScene(
                "Menu",
                LoadSceneMode.Additive);
        }
    }
}