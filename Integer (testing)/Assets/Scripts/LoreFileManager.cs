using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// LoreFileManager.cs
// Singleton manager that persists the player's lore file collection across the run.
// DontDestroyOnLoad — matching UIManager and LightingManager patterns.
// Accessible globally via LoreFileManager.Instance.
//
// Files are added by:
//   - ComputerUSBHandler (USB Lore drives)
//   - Environment pickup scripts (call LoreFileManager.Instance.AddFile())
//   - System injection (tutorial, C.E.D.R.I.C. transmissions)
//
// Place in: Assets/Scripts/

public class LoreFileManager : MonoBehaviour
{
    public static LoreFileManager Instance;

    [Header("Lore Collection")]
    // Pre-populate in Inspector for testing / guaranteed lore files.
    public List<LoreFileData> files = new List<LoreFileData>();

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

    // Adds a new lore file to the collection.
    // Returns false if a file with the same title already exists (deduplication).
    public bool AddFile(LoreFileData file)
    {
        if (file == null) return false;
        for (int i = 0; i < files.Count; i++)
        {
            if (files[i].fileTitle == file.fileTitle) return false;
        }
        files.Add(file);
        return true;
    }

    // Returns all files regardless of read state.
    public List<LoreFileData> GetAllFiles()
    {
        return files;
    }

    // Returns only unread files.
    public List<LoreFileData> GetUnreadFiles()
    {
        List<LoreFileData> unread = new List<LoreFileData>();
        for (int i = 0; i < files.Count; i++)
        {
            if (!files[i].isRead) unread.Add(files[i]);
        }
        return unread;
    }

    // Returns the count of unread files — useful for wristband notification badge.
    public int GetUnreadCount()
    {
        int count = 0;
        for (int i = 0; i < files.Count; i++)
        {
            if (!files[i].isRead) count++;
        }
        return count;
    }

    // Marks a file as read. Called by ComputerLoreMenu when the player opens a file.
    public void MarkRead(LoreFileData file)
    {
        if (file == null) return;
        file.isRead = true;
    }

    // Clears all files — call on run reset/new game.
    public void ClearAll()
    {
        files.Clear();
    }

    // -------------------------------------------------------------------------
    // ENTITY INTERACTION EXTENSION POINT
    // C.E.D.R.I.C. entities can corrupt files by calling CorruptFile().
    // ComputerLoreMenu checks file.isCorrupted and displays file.corruptedContent.
    // -------------------------------------------------------------------------
    public void CorruptFile(LoreFileData file, string corruptedContent)
    {
        if (file == null) return;
        file.isCorrupted = true;
        file.corruptedContent = corruptedContent;
    }

    // Injects a system-generated lore file (C.E.D.R.I.C. transmissions, tutorial text).
    public void InjectSystemFile(string title, string content, string sourceTag)
    {
        LoreFileData file = new LoreFileData();
        file.fileTitle = title;
        file.fileContent = content;
        file.sourceTag = sourceTag;
        file.source = LoreFileSource.System;
        file.isRead = false;
        file.isFragmented = false;
        AddFile(file);
    }
}
