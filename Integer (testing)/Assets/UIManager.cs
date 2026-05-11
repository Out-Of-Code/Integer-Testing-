using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public GameObject deathScreen;

    void Awake()
    {
        Instance = this;
    }

    public void ShowDeathScreen()
    {
        deathScreen.SetActive(true);

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        Time.timeScale = 0f;
    }
}