using UnityEngine;

public class SimpleFPSController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Gravity")]
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;
    
    [Header("Interaction")]
    public float interactDistance = 3f;
    
    [Header("Misc.")]
    public bool isHidden;
    public bool canMove = true;

    private CharacterController controller;
    private Vector3 velocity;

    private bool isGrounded;
    private float xRotation = 0f;
    
    private InteractPrompt currentPrompt;
    
    public GameObject interactImageUI;
    public float lookThreshold = 0.85f;
    
    private Door currentDoor;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleHide();

        if (!canMove)
            return;

        HandleInteraction();
        HandleMouseLook();
        HandleMovement();
    }
    void HandleHide()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            isHidden = !isHidden;

            canMove = !isHidden;

            velocity = Vector3.zero;

            controller.enabled = !isHidden;

            if (isHidden)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
    void HandleInteraction()
    {
        Collider[] hits = Physics.OverlapSphere(cameraTransform.position, interactDistance);

        Door bestDoor = null;
        float bestDot = 0.85f;

        foreach (var col in hits)
        {
            Door door = col.GetComponentInParent<Door>();
            if (door == null) continue;

            Vector3 dir = (door.transform.position - cameraTransform.position).normalized;
            float dot = Vector3.Dot(cameraTransform.forward, dir);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestDoor = door;
            }
        }

        // 🔥 ONLY update when it changes
        if (bestDoor != currentDoor)
        {
            if (currentDoor != null)
                currentDoor.SetLookedAt(false);

            currentDoor = bestDoor;

            if (currentDoor != null)
                currentDoor.SetLookedAt(true);
        }

        if (currentDoor != null && Input.GetKeyDown(KeyCode.E))
        {
            currentDoor.Interact();
        }

        if (currentDoor == null)
        {
            // ensure cleanup
            if (currentDoor != null)
                currentDoor.SetLookedAt(false);

            currentDoor = null;
        }
    }
    

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Up/down camera rotation
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Left/right player rotation
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        // Grounded check
        isGrounded = controller.isGrounded;

        // Small downward force to stay grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Ceiling check
        // Stops upward velocity when hitting a roof
        if ((controller.collisionFlags & CollisionFlags.Above) != 0 && velocity.y > 0)
        {
            velocity.y = 0f;
        }

        // Movement input
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move =
            (transform.right * moveX + transform.forward * moveZ).normalized;

        controller.Move(move * moveSpeed * Time.deltaTime);

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;

        // Apply vertical movement
        controller.Move(velocity * Time.deltaTime);
    }
}