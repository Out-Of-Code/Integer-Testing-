using UnityEngine;

public class InteractHighlight : MonoBehaviour
{
    public Renderer rend;
    Color originalColor;

    void Start()
    {
        if (rend == null)
            rend = GetComponentInChildren<Renderer>();

        originalColor = rend.material.color;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (!UIManager.Instance.settings.enableInteractHighlight)
            return;

        rend.material.color = Color.yellow;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        rend.material.color = originalColor;
    }
}