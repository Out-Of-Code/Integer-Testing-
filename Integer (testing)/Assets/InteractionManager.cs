using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("Interaction")]
    public float maxInteractDistance = 5f;

    private InteractPrompt currentPrompt;
    public LayerMask interactionLayers;

    void Update()
    {
        FindInteractable();
    }

    void FindInteractable()
    {
        currentPrompt = null;

        if (playerCamera == null)
        {
            return;
        }

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        Debug.DrawRay(
            ray.origin,
            ray.direction * maxInteractDistance,
            Color.red
        );

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxInteractDistance,
                interactionLayers))
        {

            InteractPrompt prompt =
                hit.collider.GetComponent<InteractPrompt>();

            if (prompt == null)
            {
                prompt =
                    hit.collider.GetComponentInParent<InteractPrompt>();
            }

            if (prompt == null)
            {
                return;
            }

            float distance = Vector3.Distance(
                playerCamera.transform.position,
                prompt.transform.position
            );

            if (distance <= prompt.activationDistance)
            {

                currentPrompt = prompt;
                

                if (Input.GetKeyDown(prompt.interactionKey))
                {
                    prompt.Interact();
                    Debug.Log("INTERACT CALLED");
                }
            }
        }
    }

    public InteractPrompt GetCurrentPrompt()
    {
        return currentPrompt;
    }
}