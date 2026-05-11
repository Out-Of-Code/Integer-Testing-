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
        if (!Input.GetKeyDown(KeyCode.E))
            return;

        Ray ray =
            new Ray(
                cameraTransform.position,
                cameraTransform.forward);

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactDistance))
        {
            Debug.Log(hit.collider.name);

            Door door =
                hit.collider.GetComponentInParent<Door>();

            if (door != null)
            {
                Debug.Log("FOUND DOOR");

                door.Interact();
            }
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