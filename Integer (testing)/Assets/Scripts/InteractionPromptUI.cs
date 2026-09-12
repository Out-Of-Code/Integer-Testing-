using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject uiRoot;
    public TMP_Text keyText;
    public TMP_Text promptText;

    private InteractionManager interactionManager;
    private Camera playerCamera;

    private RectTransform canvasRect;

    void Start()
    {
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        Hide();
    }

    void Update()
    {
        if (interactionManager == null)
        {
            interactionManager = FindFirstObjectByType<InteractionManager>();

            if (interactionManager == null)
            {
                Hide();
                return;
            }
        }

        if (playerCamera == null)
        {
            playerCamera = interactionManager.playerCamera;

            if (playerCamera == null)
            {
                Hide();
                return;
            }
        }

        InteractPrompt prompt = interactionManager.GetCurrentPrompt();

        if (prompt == null)
        {
            Hide();
            return;
        }

        Show(prompt);
    }

    void Show(InteractPrompt prompt)
    {
        uiRoot.SetActive(true);

        keyText.text = "[" + prompt.interactionKey.ToString() + "]";
        promptText.text = prompt.promptText;

        if (prompt.promptPoint == null)
            return;
    }

    void Hide()
    {
        uiRoot.SetActive(false);
    }
}