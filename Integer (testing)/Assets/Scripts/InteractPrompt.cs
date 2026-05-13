using UnityEngine;

public class InteractPrompt : MonoBehaviour
{
    public GameObject root;

    public void Show()
    {
        root.SetActive(true);
    }

    public void Hide()
    {
        root.SetActive(false);
    }
}