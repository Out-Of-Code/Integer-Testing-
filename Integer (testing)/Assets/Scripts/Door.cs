using System.Collections;
using UnityEngine;

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
    public int roomIndex;
    public RoomGenerator generator;
    public SPRINTController sprint;

    bool open;

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

    // =====================================================
    // INTERACTION
    // =====================================================

    public void Interact()
    {
        if (animating)
            return;

        if (locked)
        {
            OnLockedInteract();

            return;
        }

        ToggleDoor();
    }

    public void ToggleDoor()
    {
        if (animating)
            return;

        StartCoroutine(
            AnimateDoor(!open));
        if (sprint != null)
        {
            sprint.OnDoorOpened();
        }
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