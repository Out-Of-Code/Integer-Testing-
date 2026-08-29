using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string promptText = "INTERACT";
    public Transform promptPoint;

    public virtual void Interact()
    {
    }
}