using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ComputerLoreMenu.cs
// UI controller for the lore file browser displayed on the computer's ReadUSB screen.
// Attach to the same GameObject as ComputerController (or a child of it).
//
// Layout expected in readUSBScreen:
//   - File list panel  (fileListContainer + fileEntryPrefab)
//   - File detail panel (detailPanel, detailTitle, detailSource, detailContent)
//   - Unread badge     (unreadBadgeText — shows "[ X UNREAD ]")
//
// The terminal aesthetic is enforced in code:
//   - All text is uppercase
//   - Unread files are prefixed with "> "
//   - Read files are prefixed with "  "
//   - File content is passed through SanityTextCorruptor before display
//
// Place in: Assets/  (alongside ComputerController.cs)

public class ComputerLoreMenu : MonoBehaviour
{
    [Header("Computer Reference")]
    public ComputerController computer;

    [Header("File List")]
    public Transform fileListContainer;
    public GameObject fileEntryPrefab;      // Prefab: TMP_Text label + Button

    [Header("Detail Panel")]
    public GameObject detailPanel;
    public TMP_Text detailTitle;
    public TMP_Text detailSource;
    public TMP_Text detailContent;
    public TMP_Text detailReadState;        // "[ UNREAD ]" or "[ READ ]"

    [Header("Header / Badge")]
    public TMP_Text screenHeaderText;       // e.g. "C.E.D.R.I.C. FILE SYSTEM v2.1"
    public TMP_Text unreadBadgeText;        // e.g. "[ 3 UNREAD ]"
    public TMP_Text emptyStateText;         // Shown when no files collected

    [Header("Corruption")]
    // Assign the SanityTextCorruptor in the scene.
    // If null, ComputerLoreMenu will attempt FindObjectOfType at runtime.
    public SanityTextCorruptor textCorruptor;

    // Currently displayed file
    LoreFileData openFile;

    void Awake()
    {
        if (computer == null)
            computer = GetComponent<ComputerController>();
    }

    void Start()
    {
        if (textCorruptor == null)
            textCorruptor = FindObjectOfType<SanityTextCorruptor>();
    }

    // Called by ComputerController when transitioning into ReadUSB state.
    // Mirrors the pattern of ComputerController.UpdateCleanFilesUI() being called from Update.
    public void OnEnterLoreMenu()
    {
        openFile = null;
        if (detailPanel != null) detailPanel.SetActive(false);

        if (screenHeaderText != null)
            screenHeaderText.text = "C.E.D.R.I.C. FILE SYSTEM v2.1";

        RefreshFileList();
    }

    void RefreshFileList()
    {
        ClearList();

        if (LoreFileManager.Instance == null) return;

        List<LoreFileData> allFiles = LoreFileManager.Instance.GetAllFiles();

        // Update unread badge
        int unreadCount = LoreFileManager.Instance.GetUnreadCount();
        if (unreadBadgeText != null)
            unreadBadgeText.text = unreadCount > 0 ? $"[ {unreadCount} UNREAD ]" : "[ ALL READ ]";

        if (allFiles.Count == 0)
        {
            if (emptyStateText != null)
            {
                emptyStateText.gameObject.SetActive(true);
                emptyStateText.text = "NO FILES FOUND.\nINSERT A USB DRIVE TO LOAD DATA.";
            }
            return;
        }

        if (emptyStateText != null) emptyStateText.gameObject.SetActive(false);

        for (int i = 0; i < allFiles.Count; i++)
        {
            SpawnFileEntry(allFiles[i]);
        }
    }

    void SpawnFileEntry(LoreFileData file)
    {
        if (fileEntryPrefab == null || fileListContainer == null) return;

        GameObject entry = Instantiate(fileEntryPrefab, fileListContainer);
        TMP_Text label = entry.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            // Terminal aesthetic: unread files get "> " prefix, read files get "  "
            string prefix = file.isRead ? "  " : "> ";
            string corruptedTitle = ApplyTitleCorruption(file.fileTitle, file);
            label.text = $"{prefix}{corruptedTitle.ToUpper()}";
        }

        Button btn = entry.GetComponentInChildren<Button>();
        if (btn != null)
        {
            LoreFileData captured = file;
            btn.onClick.AddListener(() => OnFileSelected(captured));
        }
    }

    void ClearList()
    {
        if (fileListContainer == null) return;
        for (int i = fileListContainer.childCount - 1; i >= 0; i--)
            Destroy(fileListContainer.GetChild(i).gameObject);
    }

    // Called when the player clicks a file entry.
    public void OnFileSelected(LoreFileData file)
    {
        openFile = file;

        if (detailPanel != null) detailPanel.SetActive(true);

        // Mark as read
        if (LoreFileManager.Instance != null)
            LoreFileManager.Instance.MarkRead(file);

        // Update read state badge
        if (detailReadState != null)
            detailReadState.text = "[ READ ]";

        // Title — apply light corruption to title even at moderate sanity for atmosphere
        if (detailTitle != null)
            detailTitle.text = ApplyTitleCorruption(file.fileTitle, file).ToUpper();

        // Source tag
        if (detailSource != null)
        {
            string src = string.IsNullOrEmpty(file.sourceTag) ? "UNKNOWN SOURCE" : file.sourceTag;
            detailSource.text = $"SOURCE: {src.ToUpper()}";
        }

        // Content — full corruption pass
        if (detailContent != null)
            detailContent.text = BuildDisplayContent(file);

        // Refresh list to update read/unread prefix
        RefreshFileList();
    }

    // Builds the display string for file content, applying corruption and fragmentation.
    string BuildDisplayContent(LoreFileData file)
    {
        // If entity-corrupted, use the corrupted content directly
        if (file.isCorrupted && !string.IsNullOrEmpty(file.corruptedContent))
            return file.corruptedContent.ToUpper();

        string content = file.fileContent;
        if (string.IsNullOrEmpty(content)) content = "[FILE CONTENT MISSING]";

        // Apply intentional fragmentation gaps (isFragmented files have [REDACTED] blocks)
        if (file.isFragmented)
            content = InjectRedactions(content);

        // Apply sanity-based corruption
        if (textCorruptor != null)
            content = textCorruptor.Corrupt(content);

        return content.ToUpper();
    }

    // Applies a lighter corruption pass to file titles (preserve readability longer).
    string ApplyTitleCorruption(string title, LoreFileData file)
    {
        if (file.isCorrupted) return "##CORRUPTED##";
        if (textCorruptor == null) return title;
        // Use CorruptAtRatio with half the current ratio — titles degrade slower
        InsanityController insanity = FindObjectOfType<InsanityController>();
        if (insanity == null) return title;
        float ratio = insanity.insanity / insanity.maxInsanity * 0.5f;
        return textCorruptor.CorruptAtRatio(title, ratio);
    }

    // Replaces ~20% of words in fragmented files with [REDACTED].
    string InjectRedactions(string content)
    {
        string[] words = content.Split(' ');
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 3 && Random.value < 0.20f)
                sb.Append("[REDACTED]");
            else
                sb.Append(words[i]);
            if (i < words.Length - 1) sb.Append(' ');
        }
        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // ENTITY INTERACTION EXTENSION POINT
    // Entities can call ForceCorruptDisplay() to temporarily override the
    // displayed content with entity-injected text (e.g. C.E.D.R.I.C. taunts).
    // Call RestoreDisplay() to return to normal.
    // -------------------------------------------------------------------------
    public void ForceCorruptDisplay(string injectedText)
    {
        if (detailContent != null)
            detailContent.text = injectedText.ToUpper();
    }

    public void RestoreDisplay()
    {
        if (openFile != null)
            OnFileSelected(openFile);
    }
}
