using System.Collections.Generic;
using UnityEngine;

public enum USBDriveType
{
    Lore,
    SystemPatch,
    DefensiveUtil,
    Rickroll
}

public enum USBInsertionState
{
    Idle,
    Inserting,
    Reading,
    Complete,
    Error
}

[System.Serializable]
public class USBDriveData
{
    [Header("Identity")]
    public string driveName;
    public USBDriveType driveType;
    [TextArea] public string description;

    [Header("Lore Payload")]
    public List<LoreFileData> loreFiles;

    [Header("System Patch Payload")]
    public float systemPatchSanityBonus;

    [Header("State")]
    public bool isUsed;

    // ENTITY CORRUPTION EXTENSION POINT:
    // Add bool isCorrupted and string corruptedDriveName to allow entities
    // to tamper with drive labels before the player reads them.
}
