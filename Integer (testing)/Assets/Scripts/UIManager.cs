using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Prefabs")]
    public GameObject menuPrefab;
    public GameObject deathPrefab;
    public GameObject loadingPrefab;

    [Header("Settings")]
    public GameSettings settings = new();

    GameObject menuInstance;
    GameObject deathInstance;
    GameObject loadingInstance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        ShowMenu();
    }

    // =====================================================
    // MENU
    // =====================================================

    public void ShowMenu()
    {
        if (menuInstance == null)
        {
            menuInstance = Instantiate(menuPrefab);
        }
    }

    public void HideMenu()
    {
        if (menuInstance != null)
        {
            Destroy(menuInstance);
            menuInstance = null;
        }
    }

    // =====================================================
    // DEATH
    // =====================================================

    public void ShowDeathScreen()
    {
        if (deathInstance == null)
            deathInstance = Instantiate(deathPrefab);
    }

    public void HideDeathScreen()
    {
        if (deathInstance != null)
        {
            Destroy(deathInstance);
            deathInstance = null;
        }
    }

    // =====================================================
    // LOADING
    // =====================================================

    public void ShowLoadingScreen()
    {
        if (loadingInstance == null)
            loadingInstance = Instantiate(loadingPrefab);
    }

    public void HideLoadingScreen()
    {
        if (loadingInstance != null)
        {
            Destroy(loadingInstance);
            loadingInstance = null;
        }
    }

    // =====================================================
    // GAME FLOW
    // =====================================================

    public void StartGame()
    {
        StartCoroutine(StartGameRoutine());
    }

    IEnumerator StartGameRoutine()
    {
        Debug.Log("Starting game");

        HideMenu();

        yield return null;

        SceneManager.LoadScene("Main");
    }
}