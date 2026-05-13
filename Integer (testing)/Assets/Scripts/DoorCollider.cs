using UnityEngine;

public class DoorAutoOpen : MonoBehaviour
{
    public Door doorScript;
    public bool isOpen;

    void Start()
    {
        if (doorScript == null)
        {
            doorScript = FindObjectOfType<Door>();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (!UIManager.Instance.settings.autoOpenDoors)
            return;

        Open();
    }

    void Open()
    {
        if (isOpen) return;

        isOpen = true;
        Debug.Log("Door opened automatically");

        doorScript.Interact();
    }
}