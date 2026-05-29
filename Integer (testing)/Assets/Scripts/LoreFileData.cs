using UnityEngine;

public enum LoreFileSource
{
    USB,
    Environment,
    System
}

[System.Serializable]
public class LoreFileData
{
    [Header("Identity")]
    public string title;
    public LoreFileSource source;

    [Header("Content")]
    [TextArea(3, 10)] public string body;

    [Header("Corruption")]
    public bool isCorrupted;
    [TextArea(3, 10)] public string corruptedContent;

    [Header("State")]
    public bool isRead;

    // ENTITY CORRUPTION EXTENSION POINT:
    // Entities call LoreFileManager.CorruptFile() to set isCorrupted = true
    // and inject a corruptedContent string before the player reads the file.
}
