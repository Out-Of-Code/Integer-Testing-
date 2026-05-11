using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool playerDead;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void KillPlayer()
    {
        if (playerDead)
            return;

        playerDead = true;

        Debug.Log("Player died");

        UIManager.Instance.ShowDeathScreen();
    }
}