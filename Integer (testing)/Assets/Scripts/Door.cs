using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Door : MonoBehaviour
{
    [Header("References")]
    public Transform hinge;

    [Header("State")]
    public bool locked;

    public bool startsOpen;

    [Header("Animation")]
    public float openAngle = 90f;

    public float animationSpeed = 4f;

    [Tooltip("If true, flips swing direction")]
    public bool reverseDirection;

    [Header("Auto Close")]
    public bool autoClose;

    public float autoCloseDelay = 3f;
    public RoomGenerator generator;
    public SPRINTController sprint;
    
    public int roomIndex;
    
    public Image interactUI;

    public bool open;

    bool animating;

    Coroutine autoCloseRoutine;

    Quaternion closedRotation;

    Quaternion openedRotation;

    // =====================================================
    // UNITY
    // =====================================================

    void Start()
    {
        if (generator == null)
            generator = FindFirstObjectByType<RoomGenerator>();
        if (sprint == null)
            sprint = FindFirstObjectByType<SPRINTController>();
        if (interactUI == null)
            interactUI = FindFirstObjectByType<Image>();
        if (hinge == null)
        {
            Debug.LogError(
                $"Door '{name}' missing hinge.");

            enabled = false;

            return;
        }

        closedRotation =
            hinge.localRotation;

        float finalAngle =
            reverseDirection
            ? -openAngle
            : openAngle;

        openedRotation =
            closedRotation *
            Quaternion.Euler(
                0f,
                finalAngle,
                0f);

        if (startsOpen)
        {
            hinge.localRotation =
                openedRotation;

            open = true;
        }
    }
    public void SetLookedAt(bool state)
    {
        if (interactUI != null)
            if (open == false)
                interactUI.gameObject.SetActive(state);
    }

    // =====================================================
    // INTERACTION
    // =====================================================

    public void Interact()
    {
        if (animating || locked || open)
            return;

        ToggleDoor();

        if (generator == null)
            return;

        int newIndex = roomIndex;

        generator.SetPlayerRoom(newIndex);
        generator.OnDoorOpened(newIndex);

        if (sprint == null)
            sprint = FindFirstObjectByType<SPRINTController>();

        if (sprint != null)
        {
            sprint.OnDoorOpened(newIndex);
        }
    }

    public void ToggleDoor()
    {
        Destroy(interactUI);
        if (animating || open)
            return;

        StartCoroutine(AnimateDoor(true));
    }

    public void Open()
    {
        if (open || animating)
            return;

        StartCoroutine(
            AnimateDoor(true));
    }
    

    // =====================================================
    // ANIMATION
    // =====================================================

    IEnumerator AnimateDoor(bool targetOpen)
    {
        animating = true;

        Quaternion startRotation =
            hinge.localRotation;

        Quaternion targetRotation =
            targetOpen
            ? openedRotation
            : closedRotation;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime *
                 animationSpeed;

            hinge.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    t);

            yield return null;
        }

        hinge.localRotation =
            targetRotation;

        open = targetOpen;

        animating = false;
    }

    // =====================================================
    // LOCKED
    // =====================================================

    void OnLockedInteract()
    {
        Debug.Log("Locked");

        // future:
        // sound
        // UI popup
        // shake animation
        // etc.
    }

    // =====================================================
    // HELPERS
    // =====================================================

    public bool IsOpen()
    {
        return open;
    }

    public bool IsLocked()
    {
        return locked;
    }

    public bool IsAnimating()
    {
        return animating;
    }

    public void SetLocked(bool value)
    {
        locked = value;
    }
}