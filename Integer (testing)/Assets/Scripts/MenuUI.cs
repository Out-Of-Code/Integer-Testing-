using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class MenuUI : MonoBehaviour
{
    [Header("Seed")]
    public TMP_InputField seedInput;

    [Header("Buttons")]
    public Image seededRunImage;
    public Image collisionImage;
    public Image debugRulesImage;
    public Image autoOpenImage;
    public Image highlightImage;

    [Header("Colors")]
    public Color enabledColor = Color.green;
    public Color disabledColor = Color.gray;

    [Header("Generation")]
    public TMP_InputField roomsInput;
    public TMP_InputField operationsInput;
    public TMP_InputField cooldownInput;
    [Space(50)]
    public RoomGenerator roomGenerator;
    public UIManager MenuManager;
    public GameObject menuScreenObject;

    void Start()
    {
        if (MenuManager == null)
        {
            MenuManager = FindObjectOfType<UIManager>();
        }
        LoadSettingsIntoUI();
        if (roomGenerator == null)
        {
            roomGenerator = FindAnyObjectByType<RoomGenerator>();
        }
    }

    void LoadSettingsIntoUI()
    {
        GameSettings settings =
            UIManager.Instance.settings;

        seedInput.text =
            settings.seed.ToString();

        roomsInput.text =
            settings.roomsPerChunk.ToString();

        operationsInput.text =
            settings.operationsPerFrame.ToString();

        cooldownInput.text =
            settings.cooldown.ToString();

        RefreshVisuals();
    }

    void RefreshVisuals()
    {
        GameSettings settings =
            UIManager.Instance.settings;

        seededRunImage.color =
            settings.useSeededRun
            ? enabledColor
            : disabledColor;

        collisionImage.color =
            settings.allowCollisions
            ? enabledColor
            : disabledColor;

        debugRulesImage.color =
            settings.debugDisableRules
            ? enabledColor
            : disabledColor;

        autoOpenImage.color =
            settings.autoOpenDoors
            ? enabledColor
            : disabledColor;

        highlightImage.color =
            settings.enableInteractHighlight
            ? enabledColor
            : disabledColor;
    }

    // =====================================================
    // BUTTON TOGGLES
    // =====================================================

    public void ToggleSeededRun()
    {
        UIManager.Instance.settings.useSeededRun =
            !UIManager.Instance.settings.useSeededRun;

        RefreshVisuals();
    }

    public void ToggleCollisions()
    {
        UIManager.Instance.settings.allowCollisions =
            !UIManager.Instance.settings.allowCollisions;

        RefreshVisuals();
    }

    public void ToggleDebugRules()
    {
        UIManager.Instance.settings.debugDisableRules =
            !UIManager.Instance.settings.debugDisableRules;

        RefreshVisuals();
    }

    public void ToggleAutoOpen()
    {
        UIManager.Instance.settings.autoOpenDoors =
            !UIManager.Instance.settings.autoOpenDoors;

        RefreshVisuals();
    }

    public void ToggleHighlight()
    {
        UIManager.Instance.settings.enableInteractHighlight =
            !UIManager.Instance.settings.enableInteractHighlight;

        RefreshVisuals();
    }

    // =====================================================
    // INPUT FIELDS
    // =====================================================

    public void SetSeed(string value)
    {
        int.TryParse(
            value,
            out UIManager.Instance.settings.seed);
    }

    public void SetRooms(string value)
    {
        int.TryParse(
            value,
            out UIManager.Instance.settings.roomsPerChunk);
    }

    public void SetOperations(string value)
    {
        int.TryParse(
            value,
            out UIManager.Instance.settings.operationsPerFrame);
    }

    public void SetCooldown(string value)
    {
        float.TryParse(
            value,
            out UIManager.Instance.settings.cooldown);
    }

    // =====================================================
    // START
    // =====================================================

    public void StartGame()
    {
        Debug.Log("Starting game");
        MenuManager.HideMenu();
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene("Main");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Main") return;

        SceneManager.sceneLoaded -= OnSceneLoaded;

        RoomGenerator gen = FindObjectOfType<RoomGenerator>();
        gen.StartGeneration();
    }
}