using System.Collections.Generic;
using UnityEngine;

public class LoreFileManager : MonoBehaviour
{
    public static LoreFileManager Instance;

    [Header("Files")]
    public List<LoreFileData> files;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (files == null)
            files = new List<LoreFileData>();
    }

    public void AddFile(LoreFileData file)
    {
        if (file == null) return;
        if (!files.Contains(file))
            files.Add(file);
    }

    public List<LoreFileData> GetAllFiles()
    {
        return files;
    }

    public List<LoreFileData> GetUnreadFiles()
    {
        List<LoreFileData> unread = new List<LoreFileData>();
        foreach (LoreFileData f in files)
        {
            if (!f.isRead)
                unread.Add(f);
        }
        return unread;
    }

    public int GetUnreadCount()
    {
        int count = 0;
        foreach (LoreFileData f in files)
        {
            if (!f.isRead) count++;
        }
        return count;
    }

    public void MarkRead(LoreFileData file)
    {
        if (file == null) return;
        file.isRead = true;
    }

    public void ClearAll()
    {
        files.Clear();
    }

    public void CorruptFile(LoreFileData file, string corruptedContent = "")
    {
        if (file == null) return;
        file.isCorrupted = true;
        if (!string.IsNullOrEmpty(corruptedContent))
            file.corruptedContent = corruptedContent;
    }

    public void InjectSystemFile(string title, string body)
    {
        LoreFileData injected = new LoreFileData();
        injected.title = title;
        injected.body = body;
        injected.source = LoreFileSource.System;
        injected.isRead = false;
        injected.isCorrupted = false;
        files.Add(injected);

        // ENTITY INTERACTION EXTENSION POINT:
        // Entities call InjectSystemFile() to plant fake or corrupted system
        // messages in the player's lore log.
    }
}
