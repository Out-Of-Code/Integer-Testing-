using UnityEngine;

public class DoorInteractUI : MonoBehaviour
{
    public GameObject uiRoot;
    public Renderer rend; // optional if you want LOS checks later

    void Start()
    {
        uiRoot.SetActive(false);
    }

    public void Show()
    {
        uiRoot.SetActive(true);
    }

    public void Hide()
    {
        uiRoot.SetActive(false);
    }
}