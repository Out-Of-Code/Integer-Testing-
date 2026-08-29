using TMPro;
using UnityEngine;

public class InteractionPrompt : MonoBehaviour
{
    public GameObject prompt;
    public TMP_Text actionText;
    public Camera playerCamera;
    private bool Active;
    private Interactable UpdateTarget;

    public void Show(Interactable target)
    {
        prompt.SetActive(true);
        actionText.text = target.name;
        UpdateTarget = target;
        Active = true;
    }

    public void Hide()
    {
        prompt.SetActive(false);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Active)
        {
            Vector3 screenPosition =
                playerCamera.WorldToScreenPoint(
                    UpdateTarget.promptPoint.position
                );

            prompt.transform.position = screenPosition;
        }
    }
}
