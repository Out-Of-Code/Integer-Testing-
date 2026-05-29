using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// USBInventoryManager.cs
// Singleton manager that holds the player's USB drive collection for the current run.
// Accessible from both the computer system (ComputerUSBHandler) and the wristband
// interface (when implemented) via USBInventoryManager.Instance.
// Survives scene loads via DontDestroyOnLoad — matching UIManager/LightingManager pattern.
// Place in: Assets/Scripts/

public class USBInventoryManager : MonoBehaviour
{
    public static USBInventoryManager Instance;

    [Header("USB Inventory")]
    // Drives can be pre-populated in the Inspector for testing,
    // or added at runtime via AddDrive() when the player picks up a world USB.
    public List<USBDriveData> drives = new List<USBDriveData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Called by world USB pickup objects when the player collects a drive.
    // Returns true if the drive was successfully added (not a duplicate by name).
    public bool AddDrive(USBDriveData drive)
    {
        if (drive == null) return false;
        drives.Add(drive);
        return true;
    }

    // Returns all drives that have not yet been used.
    public List<USBDriveData> GetAvailableDrives()
    {
        List<USBDriveData> available = new List<USBDriveData>();
        for (int i = 0; i < drives.Count; i++)
        {
            if (!drives[i].isUsed)
                available.Add(drives[i]);
        }
        return available;
    }

    // Returns all drives regardless of used state (for wristband history view).
    public List<USBDriveData> GetAllDrives()
    {
        return drives;
    }

    // Marks a drive as used. Called by ComputerUSBHandler after successful insertion.
    public void MarkUsed(USBDriveData drive)
    {
        if (drive == null) return;
        drive.isUsed = true;
    }

    // Returns true if the player has at least one unused drive.
    public bool HasAvailableDrives()
    {
        for (int i = 0; i < drives.Count; i++)
        {
            if (!drives[i].isUsed) return true;
        }
        return false;
    }

    // -------------------------------------------------------------------------
    // WRISTBAND INTERFACE EXTENSION POINT
    // When the wristband UI is implemented, call GetAvailableDrives() to populate
    // the wristband's USB slot list. The wristband can call SelectDriveForInsertion()
    // below to pre-select a drive before the player sits at a computer.
    // -------------------------------------------------------------------------

    // The drive the player has "slotted" for next insertion (set via wristband or USB menu).
    // ComputerUSBHandler reads this to know which drive to insert.
    [HideInInspector] public USBDriveData selectedDrive;

    public void SelectDriveForInsertion(USBDriveData drive)
    {
        selectedDrive = drive;
    }

    public void ClearSelection()
    {
        selectedDrive = null;
    }
}
