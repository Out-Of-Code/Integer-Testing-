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
    public ComputerController currentComputer;
    private ComputerController lookedComputer;
    public bool inComputer;
    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleHide();

        if (!canMove || inComputer)
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
        ComputerController bestComputer = null;

        float bestDot = 0.85f;

        foreach (var col in hits)
        {
            Vector3 dir = (col.transform.position - cameraTransform.position).normalized;
            float dot = Vector3.Dot(cameraTransform.forward, dir);

            if (dot < bestDot) continue;

            // DOOR CHECK
            Door door = col.GetComponentInParent<Door>();
            if (door != null)
            {
                bestDoor = door;
                bestDot = dot;
                continue;
            }

            // COMPUTER CHECK
            ComputerInteractHitbox hit = col.GetComponent<ComputerInteractHitbox>();

            if (hit != null)
            {
                bestComputer = hit.computer;
            }
        }

        // DOOR logic (unchanged)
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

        // COMPUTER logic
        lookedComputer = bestComputer;

        if (lookedComputer != null && Input.GetKeyDown(KeyCode.E))
        {
            EnterComputer(lookedComputer);
        }
    }
    void EnterComputer(ComputerController computer)
    {
        Debug.Log("ENTER COMPUTER");

        canMove = false;
        inComputer = true;

        controller.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        currentComputer = computer;

        // SNAP CAMERA
        cameraTransform.SetParent(null); // IMPORTANT: break player follow
        cameraTransform.position = computer.CameraPoint.position;
        cameraTransform.rotation = computer.CameraPoint.rotation;

        computer.gameObject.SetActive(true);

        computer.OnEnterComputer();
    }
    public void ExitComputerMode()
    {
        Debug.Log("EXIT COMPUTER");

        canMove = true;
        inComputer = false;

        controller.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // reattach camera back to player
        cameraTransform.SetParent(transform);
        cameraTransform.localPosition = new Vector3(0, 1.6f, 0); // adjust if needed
        cameraTransform.localRotation = Quaternion.identity;
    }
    

    void HandleMouseLook()
    {
        if (inComputer) return;
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