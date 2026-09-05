using UnityEngine;
using UnityEngine.Events;

public class InteractPrompt : MonoBehaviour
{
    [Header("Prompt")]
    public string promptText = "INTERACT";
    public KeyCode interactionKey = KeyCode.E;
    public float activationDistance = 3f;

    [Tooltip("Where the prompt appears on screen.")]
    public Transform promptPoint;

    [Header("Interaction")]
    public UnityEvent onInteract;

    public void Interact()
    {
        onInteract?.Invoke();
    }
}