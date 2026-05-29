using System.Collections.Generic;
using UnityEngine;

public class USBInventoryManager : MonoBehaviour
{
    public static USBInventoryManager Instance;

    [Header("Inventory")]
    public List<USBDriveData> drives;

    private USBDriveData selectedDrive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (drives == null)
            drives = new List<USBDriveData>();
    }

    public void AddDrive(USBDriveData drive)
    {
        if (drive == null) return;
        drives.Add(drive);
    }

    public List<USBDriveData> GetAvailableDrives()
    {
        List<USBDriveData> available = new List<USBDriveData>();
        foreach (USBDriveData d in drives)
        {
            if (!d.isUsed)
                available.Add(d);
        }
        return available;
    }

    public List<USBDriveData> GetAllDrives()
    {
        return drives;
    }

    public void MarkUsed(USBDriveData drive)
    {
        if (drive == null) return;
        drive.isUsed = true;
    }

    public bool HasAvailableDrives()
    {
        foreach (USBDriveData d in drives)
        {
            if (!d.isUsed) return true;
        }
        return false;
    }

    public void SelectDriveForInsertion(USBDriveData drive)
    {
        selectedDrive = drive;
    }

    public USBDriveData GetSelectedDrive()
    {
        return selectedDrive;
    }

    public void ClearSelection()
    {
        selectedDrive = null;
    }

    // WRISTBAND EXTENSION POINT:
    // Call WristbandManager.Instance?.OnUSBEvent(drive) here when drives are
    // added or marked used to track USB activity on the player's wristband.
}
