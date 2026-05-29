using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ComputerUSBHandler.cs
// Handles USB drive insertion into a computer terminal.
// Attach to the same GameObject as ComputerController.
// Hooks into the existing ReadUSB screen (ComputerController.readUSBScreen).
//
// State machine: Idle → Inserting → Reading → Complete / Error
// Each state transition is a coroutine, matching ComputerController's pattern.
//
// Place in: Assets/  (alongside ComputerController.cs)

public class ComputerUSBHandler : MonoBehaviour
{
    [Header("Computer Reference")]
    public ComputerController computer;

    [Header("USB List UI")]
    // Assign the ScrollRect content transform inside readUSBScreen.
    // ComputerUSBHandler instantiates usbEntryPrefab here for each available drive.
    public Transform usbListContainer;
    public GameObject usbEntryPrefab;       // Prefab: TMP_Text label + select button

    [Header("USB Detail Panel")]
    // Sub-panel inside readUSBScreen shown after a drive is selected.
    public GameObject detailPanel;
    public TMP_Text detailNameText;
    public TMP_Text detailTypeText;
    public TMP_Text detailFlavorText;
    public TMP_Text detailStatusText;       // "READY", "ALREADY USED", "INSERT? [Y/N]"

    [Header("Insertion Feedback")]
    // Status line shown during the insertion coroutine.
    public TMP_Text insertionStateText;
    // Timing
    public float insertingDuration = 1.2f;  // Seconds in Inserting state
    public float readingDuration = 2.0f;    // Seconds in Reading state

    [Header("Lore Preview")]
    // Brief preview panel shown after a Lore USB completes.
    public GameObject lorePreviewPanel;
    public TMP_Text lorePreviewTitle;
    public TMP_Text lorePreviewSnippet;     // First 120 chars of lore content
    public float lorePreviewDuration = 4f;

    // Internal state
    USBInsertionState insertionState;
    USBDriveData selectedDrive;
    bool isInserting;

    InsanityController playerInsanity;

    void Awake()
    {
        if (computer == null)
            computer = GetComponent<ComputerController>();
    }

    void Update()
    {
        if (playerInsanity == null)
            playerInsanity = FindObjectOfType<InsanityController>();
    }

    // Called by ComputerController (or ComputerInputController) when the player
    // navigates to the ReadUSB screen. Populates the drive list.
    public void OnEnterReadUSBScreen()
    {
        ClearList();
        SetInsertionState(USBInsertionState.Idle);

        if (detailPanel != null) detailPanel.SetActive(false);
        if (lorePreviewPanel != null) lorePreviewPanel.SetActive(false);

        if (USBInventoryManager.Instance == null) return;

        List<USBDriveData> drives = USBInventoryManager.Instance.GetAllDrives();

        if (drives.Count == 0)
        {
            // Show empty state message via insertionStateText
            if (insertionStateText != null)
                insertionStateText.text = "NO USB DRIVES IN INVENTORY";
            return;
        }

        for (int i = 0; i < drives.Count; i++)
        {
            SpawnDriveEntry(drives[i]);
        }
    }

    void SpawnDriveEntry(USBDriveData drive)
    {
        if (usbEntryPrefab == null || usbListContainer == null) return;

        GameObject entry = Instantiate(usbEntryPrefab, usbListContainer);
        TMP_Text label = entry.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            string usedTag = drive.isUsed ? " [USED]" : "";
            label.text = $"> {drive.usbName}{usedTag}";
        }

        // Wire up the button — capture drive reference for lambda
        UnityEngine.UI.Button btn = entry.GetComponentInChildren<UnityEngine.UI.Button>();
        if (btn != null)
        {
            USBDriveData captured = drive;
            btn.onClick.AddListener(() => OnDriveSelected(captured));
        }
    }

    void ClearList()
    {
        if (usbListContainer == null) return;
        for (int i = usbListContainer.childCount - 1; i >= 0; i--)
            Destroy(usbListContainer.GetChild(i).gameObject);
    }

    // Called when the player clicks a drive entry in the list.
    public void OnDriveSelected(USBDriveData drive)
    {
        if (isInserting) return;
        selectedDrive = drive;

        if (detailPanel != null) detailPanel.SetActive(true);
        if (detailNameText != null) detailNameText.text = drive.usbName;
        if (detailTypeText != null) detailTypeText.text = drive.driveType.ToString().ToUpper();
        if (detailFlavorText != null) detailFlavorText.text = drive.flavorText;

        if (drive.isUsed)
        {
            if (detailStatusText != null) detailStatusText.text = "STATUS: ALREADY USED";
        }
        else
        {
            if (detailStatusText != null) detailStatusText.text = "INSERT? [Y] YES  [N] NO";
        }

        SetInsertionState(USBInsertionState.Idle);
    }

    // Called by ComputerInputController when player presses Y/Enter while
    // in ReadUSB state and a drive is selected.
    public void TryInsertSelected()
    {
        if (isInserting) return;
        if (selectedDrive == null)
        {
            SetInsertionState(USBInsertionState.Error);
            if (insertionStateText != null) insertionStateText.text = "ERROR: NO DRIVE SELECTED";
            return;
        }
        if (selectedDrive.isUsed)
        {
            SetInsertionState(USBInsertionState.Error);
            if (insertionStateText != null) insertionStateText.text = "ERROR: DRIVE ALREADY USED";
            return;
        }
        StartCoroutine(InsertionRoutine(selectedDrive));
    }

    IEnumerator InsertionRoutine(USBDriveData drive)
    {
        isInserting = true;

        // --- INSERTING ---
        SetInsertionState(USBInsertionState.Inserting);
        if (insertionStateText != null) insertionStateText.text = "INSERTING DRIVE...";
        yield return new WaitForSeconds(insertingDuration);

        // --- READING ---
        SetInsertionState(USBInsertionState.Reading);
        if (insertionStateText != null) insertionStateText.text = $"READING: {drive.usbName}";
        yield return new WaitForSeconds(readingDuration);

        // --- APPLY EFFECT ---
        bool success = ApplyEffect(drive);

        if (success)
        {
            // Mark consumed
            if (USBInventoryManager.Instance != null)
                USBInventoryManager.Instance.MarkUsed(drive);

            SetInsertionState(USBInsertionState.Complete);
            if (insertionStateText != null) insertionStateText.text = "TRANSFER COMPLETE";
            if (detailStatusText != null) detailStatusText.text = "STATUS: USED";

            // Refresh list to show [USED] tag
            OnEnterReadUSBScreen();
        }
        else
        {
            SetInsertionState(USBInsertionState.Error);
            if (insertionStateText != null) insertionStateText.text = "ERROR: TRANSFER FAILED";
        }

        isInserting = false;
    }

    // Applies the drive's effect based on its type.
    // Returns true on success, false on failure.
    bool ApplyEffect(USBDriveData drive)
    {
        switch (drive.driveType)
        {
            case USBDriveType.Lore:
                return ApplyLoreEffect(drive);

            case USBDriveType.SystemPatch:
                return ApplySystemPatchEffect(drive);

            case USBDriveType.DefensiveUtil:
                return ApplyDefensiveUtilEffect(drive);

            case USBDriveType.Rickroll:
                return ApplyRickrollEffect(drive);

            default:
                return false;
        }
    }

    bool ApplyLoreEffect(USBDriveData drive)
    {
        if (LoreFileManager.Instance == null) return false;

        LoreFileData file = new LoreFileData();
        file.fileTitle = string.IsNullOrEmpty(drive.loreFileTitle) ? drive.usbName : drive.loreFileTitle;
        file.fileContent = drive.loreFileContent;
        file.sourceTag = drive.loreSourceTag;
        file.source = LoreFileSource.USB;
        file.sourceDetail = drive.usbName;
        file.isRead = false;
        file.isFragmented = false;

        LoreFileManager.Instance.AddFile(file);

        // Show brief preview
        StartCoroutine(ShowLorePreview(file));
        return true;
    }

    bool ApplySystemPatchEffect(USBDriveData drive)
    {
        if (playerInsanity == null)
            playerInsanity = FindObjectOfType<InsanityController>();

        if (playerInsanity != null && drive.sanityRestoreAmount > 0f)
            playerInsanity.ReduceInsanity(drive.sanityRestoreAmount);

        // -------------------------------------------------------------------------
        // EXTENSION POINT: insanity resistance duration
        // When an InsanityResistanceController or buff system is implemented,
        // pass drive.insanityResistDuration to it here.
        // -------------------------------------------------------------------------

        return true;
    }

    bool ApplyDefensiveUtilEffect(USBDriveData drive)
    {
        // -------------------------------------------------------------------------
        // EXTENSION POINT: Defensive utility registration
        // When the wristband equip system is implemented, call:
        //   WristbandManager.Instance.RegisterUtility(drive.utilityName, drive.utilityDescription)
        // For now, log the registration so it's visible during development.
        // -------------------------------------------------------------------------
        Debug.Log($"[ComputerUSBHandler] Defensive utility registered: {drive.utilityName}");
        return true;
    }

    bool ApplyRickrollEffect(USBDriveData drive)
    {
        // Rickroll: clear insanity spike and suppress passive gain briefly.
        // Thematically resets hallucinations — hook visual/audio effects here.
        if (playerInsanity == null)
            playerInsanity = FindObjectOfType<InsanityController>();

        if (playerInsanity != null)
        {
            // Reduce insanity by 25% of max as a "reset"
            playerInsanity.ReduceInsanity(playerInsanity.maxInsanity * 0.25f);
        }

        // -------------------------------------------------------------------------
        // EXTENSION POINT: Hallucination system reset
        // When a HallucinationController exists, call:
        //   HallucinationController.Instance.ClearAllHallucinations()
        // -------------------------------------------------------------------------

        if (insertionStateText != null)
            insertionStateText.text = "NEVER GONNA GIVE YOU UP...";

        return true;
    }

    IEnumerator ShowLorePreview(LoreFileData file)
    {
        if (lorePreviewPanel == null) yield break;

        lorePreviewPanel.SetActive(true);
        if (lorePreviewTitle != null) lorePreviewTitle.text = file.fileTitle;
        if (lorePreviewSnippet != null)
        {
            string snippet = file.fileContent;
            if (snippet != null && snippet.Length > 120)
                snippet = snippet.Substring(0, 120) + "...";
            lorePreviewSnippet.text = snippet;
        }

        yield return new WaitForSeconds(lorePreviewDuration);
        lorePreviewPanel.SetActive(false);
    }

    void SetInsertionState(USBInsertionState newState)
    {
        insertionState = newState;
        // Hook: audio/visual feedback per state can be added here.
        // e.g. play a different AudioClip per state, flash a UI element, etc.
    }

    // -------------------------------------------------------------------------
    // ENTITY INTERACTION EXTENSION POINT
    // C.E.D.R.I.C. entities can call CorruptInsertion() to interrupt an active
    // insertion, corrupt the drive data, or inject false lore files.
    // -------------------------------------------------------------------------
    public void CorruptInsertion()
    {
        if (!isInserting) return;
        StopAllCoroutines();
        isInserting = false;
        SetInsertionState(USBInsertionState.Error);
        if (insertionStateText != null)
            insertionStateText.text = "ERROR: SIGNAL CORRUPTED BY EXTERNAL PROCESS";
    }
}
