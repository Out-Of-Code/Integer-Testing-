using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComputerController : MonoBehaviour
{
    public enum ComputerState
    {
        Off,
        MainMenu,
        SelectionMenu,
        Video,
        CleanFiles,
        ReadUSB,
        Save
    }
    public enum USBType
    {
        Lore,
        Upgrade
    }
    public enum ComputerCommandType
    {
        Enter,
        Back,
        Hide,
        Video,
        CleanFiles,
        Save,
        ReadUSB
    }

    public class ComputerCommand
    {
        public ComputerCommandType type;
        public ComputerCommand(ComputerCommandType type) { this.type = type; }
    }

    [System.Serializable]
    public class USBData
    {
        public string usbName;
        public USBType type;
        [TextArea] public string loreText;
        public Sprite image;
        public bool isWorldUpgrade;
    }

    public ComputerState state;

    [Header("UI Screens")]
    public GameObject offScreen;
    public GameObject menuScreen;
    public GameObject loadingScreen;
    public GameObject selectionScreen;
    public GameObject videoScreen;
    public GameObject cleanFilesScreen;
    public GameObject readUSBScreen;
    public GameObject saveScreen;

    [Header("Overlays")]
    public GameObject errorOverlay;
    public TMP_Text errorText;
    public GameObject hiddenOverlay;

    [Header("Player")]
    public Transform player;
    public Transform hiddenPoint;
    public Transform CameraPoint;

    [Header("Clean Files")]
    public TMP_Text cleanFilesButtonText;
    public TMP_Text insanityText;
    public float cleanRate = 5f;

    [Header("Read USB — Drive List Panel")]
    public GameObject driveListPanel;
    public Transform driveListContainer;
    public GameObject driveEntryPrefab;
    public TMP_Text noDrivesText;

    [Header("Read USB — Drive Detail Panel")]
    public GameObject driveDetailPanel;
    public TMP_Text driveDetailNameText;
    public TMP_Text driveDetailTypeText;
    public TMP_Text driveDetailDescText;
    public Button insertDriveButton;
    public TMP_Text insertDriveButtonText;

    [Header("Read USB — Lore File List Panel")]
    public GameObject loreFileListPanel;
    public Transform loreFileListContainer;
    public GameObject loreFileEntryPrefab;
    public TMP_Text noFilesText;

    [Header("Read USB — Lore File Detail Panel")]
    public GameObject loreFileDetailPanel;
    public TMP_Text loreFileDetailTitleText;
    public TMP_Text loreFileDetailBodyText;
    public TMP_Text loreFileDetailSourceText;
    public TMP_Text loreFileDetailCorruptedBadge;

    [Header("Read USB — Insertion Progress")]
    public GameObject insertionProgressPanel;
    public TMP_Text insertionStatusText;
    public Slider insertionProgressBar;

    private InsanityController playerInsanity;
    private USBDriveData selectedDrive;
    private LoreFileData selectedFile;

    bool isCleaningFiles;
    bool isTransitioning;
    bool isHidden;
    bool videoUnlocked;
    bool isInserting;

    public List<USBData> usbInventory;

    Dictionary<ComputerState, Dictionary<ComputerState, List<ComputerCommandType>>> links;

    void Awake() { BuildGraph(); }

    void Start()
    {
        state = ComputerState.Off;
        DisableAllScreens();
        if (offScreen) offScreen.SetActive(true);
        if (errorOverlay) errorOverlay.SetActive(false);
        if (hiddenOverlay) hiddenOverlay.SetActive(false);
    }

    void Update()
    {
        if (playerInsanity == null)
            playerInsanity = FindObjectOfType<InsanityController>();
        if (state == ComputerState.CleanFiles)
            UpdateCleanFilesUI();
    }

    void UpdateCleanFilesUI()
    {
        if (playerInsanity == null) return;
        if (insanityText != null)
            insanityText.text = $"FILE CLUTTER: {playerInsanity.insanity / 10f:000.00}%";
        if (isCleaningFiles)
            playerInsanity.ReduceInsanity(cleanRate * Time.deltaTime);
    }

    public void ToggleCleaningFiles()
    {
        isCleaningFiles = !isCleaningFiles;
        if (cleanFilesButtonText != null)
            cleanFilesButtonText.text = isCleaningFiles ? "STOP CLEANING" : "CLEAN FILES";
    }

    void BuildGraph()
    {
        links = new Dictionary<ComputerState, Dictionary<ComputerState, List<ComputerCommandType>>>();
        links[ComputerState.Off] = new Dictionary<ComputerState, List<ComputerCommandType>>
        {
            { ComputerState.MainMenu, new List<ComputerCommandType> { ComputerCommandType.Enter } }
        };
        links[ComputerState.MainMenu] = new Dictionary<ComputerState, List<ComputerCommandType>>
        {
            { ComputerState.SelectionMenu, new List<ComputerCommandType> { ComputerCommandType.Enter } }
        };
        links[ComputerState.SelectionMenu] = new Dictionary<ComputerState, List<ComputerCommandType>>
        {
            { ComputerState.Video, new List<ComputerCommandType> { ComputerCommandType.Video } },
            { ComputerState.CleanFiles, new List<ComputerCommandType> { ComputerCommandType.CleanFiles } },
            { ComputerState.ReadUSB, new List<ComputerCommandType> { ComputerCommandType.ReadUSB } },
            { ComputerState.Save, new List<ComputerCommandType> { ComputerCommandType.Save } }
        };
    }

    public void Execute(ComputerCommand cmd)
    {
        if (isTransitioning) return;
        if (cmd.type == ComputerCommandType.Back) { HandleBack(); return; }
        if (state == ComputerState.MainMenu && cmd.type == ComputerCommandType.Enter)
        { StartCoroutine(LoadToSelection()); return; }
        if (state == ComputerState.SelectionMenu && cmd.type == ComputerCommandType.Hide)
        { StartCoroutine(HideRoutine()); return; }
        if (cmd.type == ComputerCommandType.Video && !isHidden)
        { ShowError("ERROR: YOU ARE NOT HIDING"); return; }
        if (isHidden && (cmd.type == ComputerCommandType.CleanFiles || cmd.type == ComputerCommandType.ReadUSB))
        { ShowError("ERROR: YOU ARE HIDING"); return; }
        if (cmd.type == ComputerCommandType.Save)
        { ShowError("ERROR: NOT IN SAFE ROOM"); return; }
        if (links == null || !links.ContainsKey(state)) return;
        var options = links[state];
        foreach (var kvp in options)
        {
            if (kvp.Value.Contains(cmd.type)) { SetState(kvp.Key); return; }
        }
        ShowError("INVALID INPUT");
    }

    void HandleBack()
    {
        switch (state)
        {
            case ComputerState.SelectionMenu:
                SetState(ComputerState.MainMenu); break;
            case ComputerState.MainMenu:
                if (isHidden) { ShowError("ERROR: YOU ARE HIDING"); return; }
                ExitComputer(); break;
            case ComputerState.Video:
            case ComputerState.ReadUSB:
            case ComputerState.Save:
                SetState(ComputerState.SelectionMenu); break;
            case ComputerState.CleanFiles:
                if (isCleaningFiles) { ShowError("ERROR: CLEANING FILES"); return; }
                SetState(ComputerState.SelectionMenu); break;
        }
    }

    void SetState(ComputerState newState)
    {
        if (!isTransitioning) StartCoroutine(TransitionToState(newState));
    }

    IEnumerator TransitionToState(ComputerState newState)
    {
        isTransitioning = true;
        DisableAllScreens();
        if (loadingScreen) loadingScreen.SetActive(true);
        yield return new WaitForSeconds(3f);
        if (loadingScreen) loadingScreen.SetActive(false);
        state = newState;
        switch (state)
        {
            case ComputerState.Off: offScreen.SetActive(true); break;
            case ComputerState.MainMenu: menuScreen.SetActive(true); break;
            case ComputerState.SelectionMenu: selectionScreen.SetActive(true); break;
            case ComputerState.Video: videoScreen.SetActive(true); break;
            case ComputerState.CleanFiles:
                cleanFilesScreen.SetActive(true);
                if (cleanFilesButtonText != null)
                    cleanFilesButtonText.text = isCleaningFiles ? "STOP CLEANING" : "CLEAN FILES";
                break;
            case ComputerState.ReadUSB:
                readUSBScreen.SetActive(true);
                OnEnterReadUSBScreen();
                break;
            case ComputerState.Save: saveScreen.SetActive(true); break;
        }
        isTransitioning = false;
    }

    IEnumerator HideRoutine()
    {
        isTransitioning = true;
        DisableAllScreens();
        loadingScreen.SetActive(true);
        yield return new WaitForSeconds(3f);
        loadingScreen.SetActive(false);
        isHidden = !isHidden;
        if (player != null && hiddenPoint != null && isHidden == true)
            player.position = hiddenPoint.position;
        if (hiddenOverlay) hiddenOverlay.SetActive(isHidden);
        state = ComputerState.SelectionMenu;
        selectionScreen.SetActive(true);
        isTransitioning = false;
    }

    IEnumerator LoadToSelection()
    {
        isTransitioning = true;
        menuScreen.SetActive(false);
        loadingScreen.SetActive(true);
        yield return new WaitForSeconds(3f);
        loadingScreen.SetActive(false);
        state = ComputerState.SelectionMenu;
        selectionScreen.SetActive(true);
        isTransitioning = false;
    }

    void DisableAllScreens()
    {
        if (offScreen) offScreen.SetActive(false);
        if (menuScreen) menuScreen.SetActive(false);
        if (loadingScreen) loadingScreen.SetActive(false);
        if (selectionScreen) selectionScreen.SetActive(false);
        if (videoScreen) videoScreen.SetActive(false);
        if (cleanFilesScreen) cleanFilesScreen.SetActive(false);
        if (readUSBScreen) readUSBScreen.SetActive(false);
        if (saveScreen) saveScreen.SetActive(false);
    }

    public void OnEnterComputer()
    {
        if (state != ComputerState.Off) return;
        DisableAllScreens();
        StartCoroutine(TransitionToState(ComputerState.MainMenu));
        FindAnyObjectByType<ComputerInteractHitbox>().gameObject.SetActive(false);
    }

    void ExitComputer()
    {
        if (isHidden) { ShowError("ERROR: YOU ARE HIDING"); return; }
        if (state != ComputerState.MainMenu) return;
        StopAllCoroutines();
        state = ComputerState.Off;
        DisableAllScreens();
        if (offScreen) offScreen.SetActive(true);
        isTransitioning = false;
        SimpleFPSController playerController = FindObjectOfType<SimpleFPSController>();
        if (playerController != null) playerController.ExitComputerMode();
        FindAnyObjectByType<ComputerInteractHitbox>().gameObject.SetActive(true);
    }

    Coroutine errorRoutine;
    void ShowError(string message)
    {
        if (errorOverlay == null || errorText == null) return;
        if (errorRoutine != null) StopCoroutine(errorRoutine);
        errorOverlay.SetActive(true);
        errorText.text = message;
        errorRoutine = StartCoroutine(HideErrorRoutine());
    }

    IEnumerator HideErrorRoutine()
    {
        yield return new WaitForSeconds(2f);
        if (errorOverlay != null) errorOverlay.SetActive(false);
        errorRoutine = null;
    }

    // READ USB SCREEN — DRIVE LIST

    public void OnEnterReadUSBScreen()
    {
        selectedDrive = null;
        selectedFile = null;

        if (driveDetailPanel) driveDetailPanel.SetActive(false);
        if (loreFileListPanel) loreFileListPanel.SetActive(false);
        if (loreFileDetailPanel) loreFileDetailPanel.SetActive(false);
        if (insertionProgressPanel) insertionProgressPanel.SetActive(false);
        if (driveListPanel) driveListPanel.SetActive(true);

        ClearDriveList();

        if (USBInventoryManager.Instance == null)
        {
            if (noDrivesText) noDrivesText.text = "NO USB MANAGER FOUND";
            if (noDrivesText) noDrivesText.gameObject.SetActive(true);
            return;
        }

        List<USBDriveData> available = USBInventoryManager.Instance.GetAvailableDrives();

        if (available == null || available.Count == 0)
        {
            if (noDrivesText) noDrivesText.text = "NO USB DRIVES DETECTED";
            if (noDrivesText) noDrivesText.gameObject.SetActive(true);
            return;
        }

        if (noDrivesText) noDrivesText.gameObject.SetActive(false);

        foreach (USBDriveData drive in available)
            SpawnDriveEntry(drive);
    }

    void SpawnDriveEntry(USBDriveData drive)
    {
        if (driveEntryPrefab == null || driveListContainer == null) return;
        GameObject entry = Instantiate(driveEntryPrefab, driveListContainer);

        TMP_Text label = entry.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            string typeTag = drive.driveType.ToString().ToUpper();
            label.text = $"[{typeTag}] {drive.driveName}";
            // ENTITY CORRUPTION EXTENSION POINT:
            // label.text = SanityTextCorruptor.CorruptText(label.text, playerInsanity != null ? playerInsanity.insanity : 0f, 1000f);
        }

        Button btn = entry.GetComponent<Button>();
        if (btn != null)
        {
            USBDriveData captured = drive;
            btn.onClick.AddListener(() => OnDriveSelected(captured));
        }
    }

    void ClearDriveList()
    {
        if (driveListContainer == null) return;
        foreach (Transform child in driveListContainer)
            Destroy(child.gameObject);
    }

    public void OnDriveSelected(USBDriveData drive)
    {
        selectedDrive = drive;
        USBInventoryManager.Instance?.SelectDriveForInsertion(drive);

        if (driveListPanel) driveListPanel.SetActive(false);
        if (driveDetailPanel) driveDetailPanel.SetActive(true);

        if (driveDetailNameText) driveDetailNameText.text = drive.driveName;
        if (driveDetailTypeText) driveDetailTypeText.text = drive.driveType.ToString().ToUpper();
        if (driveDetailDescText) driveDetailDescText.text = drive.description;

        bool alreadyUsed = drive.isUsed;
        if (insertDriveButton) insertDriveButton.interactable = !alreadyUsed;
        if (insertDriveButtonText)
            insertDriveButtonText.text = alreadyUsed ? "ALREADY READ" : "INSERT DRIVE";
    }

    public void BackToDriveList()
    {
        if (driveDetailPanel) driveDetailPanel.SetActive(false);
        if (driveListPanel) driveListPanel.SetActive(true);
        selectedDrive = null;
    }

    // READ USB SCREEN — INSERTION

    public void InsertSelectedDrive()
    {
        if (selectedDrive == null) { ShowError("ERROR: NO DRIVE SELECTED"); return; }
        if (isInserting) return;
        StartCoroutine(InsertionRoutine(selectedDrive));
    }

    IEnumerator InsertionRoutine(USBDriveData drive)
    {
        isInserting = true;

        if (driveDetailPanel) driveDetailPanel.SetActive(false);
        if (insertionProgressPanel) insertionProgressPanel.SetActive(true);

        float duration = 4f;
        float elapsed = 0f;

        string[] statusMessages = new string[]
        {
            "READING DRIVE...",
            "VERIFYING INTEGRITY...",
            "DECRYPTING CONTENTS...",
            "LOADING FILES..."
        };

        int msgIndex = 0;
        if (insertionStatusText) insertionStatusText.text = statusMessages[0];
        if (insertionProgressBar) insertionProgressBar.value = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (insertionProgressBar) insertionProgressBar.value = t;

            int newIndex = Mathf.FloorToInt(t * statusMessages.Length);
            newIndex = Mathf.Clamp(newIndex, 0, statusMessages.Length - 1);
            if (newIndex != msgIndex)
            {
                msgIndex = newIndex;
                if (insertionStatusText) insertionStatusText.text = statusMessages[msgIndex];
            }

            yield return null;
        }

        if (insertionProgressBar) insertionProgressBar.value = 1f;
        if (insertionStatusText) insertionStatusText.text = "COMPLETE";
        yield return new WaitForSeconds(0.75f);

        ApplyEffect(drive);
        USBInventoryManager.Instance?.MarkUsed(drive);

        if (insertionProgressPanel) insertionProgressPanel.SetActive(false);
        isInserting = false;

        if (drive.driveType == USBDriveType.Lore)
            OnEnterLoreMenu(drive);
        else
            OnEnterReadUSBScreen();
    }

    void ApplyEffect(USBDriveData drive)
    {
        switch (drive.driveType)
        {
            case USBDriveType.Lore:
                ApplyLoreEffect(drive);
                break;
            case USBDriveType.SystemPatch:
                ApplySystemPatchEffect(drive);
                break;
            case USBDriveType.DefensiveUtil:
                ApplyDefensiveUtilEffect(drive);
                break;
            case USBDriveType.Rickroll:
                ApplyRickrollEffect(drive);
                break;
        }
    }

    void ApplyLoreEffect(USBDriveData drive)
    {
        if (LoreFileManager.Instance == null) return;
        foreach (LoreFileData file in drive.loreFiles)
            LoreFileManager.Instance.AddFile(file);
    }

    void ApplySystemPatchEffect(USBDriveData drive)
    {
        // ENTITY CORRUPTION EXTENSION POINT:
        // System patches could reduce entity aggression or unlock computer functions.
        if (playerInsanity != null)
            playerInsanity.ReduceInsanity(drive.systemPatchSanityBonus);
    }

    void ApplyDefensiveUtilEffect(USBDriveData drive)
    {
        // ENTITY CORRUPTION EXTENSION POINT:
        // Defensive utilities could grant temporary immunity to entity corruption.
        ShowError("DEFENSIVE UTILITY INSTALLED");
    }

    void ApplyRickrollEffect(USBDriveData drive)
    {
        ShowError("ERROR: NEVER GONNA GIVE YOU UP");
    }

    // READ USB SCREEN — LORE FILE LIST

    public void OnEnterLoreMenu(USBDriveData sourceDrive)
    {
        selectedFile = null;

        if (driveListPanel) driveListPanel.SetActive(false);
        if (driveDetailPanel) driveDetailPanel.SetActive(false);
        if (loreFileDetailPanel) loreFileDetailPanel.SetActive(false);
        if (loreFileListPanel) loreFileListPanel.SetActive(true);

        RefreshFileList(sourceDrive);
    }

    void RefreshFileList(USBDriveData sourceDrive)
    {
        ClearFileList();

        List<LoreFileData> files = sourceDrive != null
            ? sourceDrive.loreFiles
            : (LoreFileManager.Instance != null ? LoreFileManager.Instance.GetAllFiles() : null);

        if (files == null || files.Count == 0)
        {
            if (noFilesText) noFilesText.text = "NO FILES FOUND";
            if (noFilesText) noFilesText.gameObject.SetActive(true);
            return;
        }

        if (noFilesText) noFilesText.gameObject.SetActive(false);

        foreach (LoreFileData file in files)
            SpawnFileEntry(file);
    }

    void SpawnFileEntry(LoreFileData file)
    {
        if (loreFileEntryPrefab == null || loreFileListContainer == null) return;
        GameObject entry = Instantiate(loreFileEntryPrefab, loreFileListContainer);

        TMP_Text label = entry.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            string title = file.isCorrupted ? ApplyTitleCorruption(file.title) : file.title;
            string readTag = file.isRead ? "" : " [NEW]";
            label.text = title + readTag;
        }

        Button btn = entry.GetComponent<Button>();
        if (btn != null)
        {
            LoreFileData captured = file;
            btn.onClick.AddListener(() => OnFileSelected(captured));
        }
    }

    void ClearFileList()
    {
        if (loreFileListContainer == null) return;
        foreach (Transform child in loreFileListContainer)
            Destroy(child.gameObject);
    }

    public void BackToDriveDetail()
    {
        if (loreFileListPanel) loreFileListPanel.SetActive(false);
        if (driveDetailPanel) driveDetailPanel.SetActive(true);
    }

    // READ USB SCREEN — LORE FILE DETAIL

    public void OnFileSelected(LoreFileData file)
    {
        selectedFile = file;
        LoreFileManager.Instance?.MarkRead(file);

        if (loreFileListPanel) loreFileListPanel.SetActive(false);
        if (loreFileDetailPanel) loreFileDetailPanel.SetActive(true);

        ShowLorePreview(file);
    }

    void ShowLorePreview(LoreFileData file)
    {
        if (loreFileDetailTitleText)
        {
            string title = file.isCorrupted ? ApplyTitleCorruption(file.title) : file.title;
            loreFileDetailTitleText.text = title;
        }

        if (loreFileDetailBodyText)
        {
            string body = file.isCorrupted
                ? (string.IsNullOrEmpty(file.corruptedContent) ? ApplyTitleCorruption(file.body) : file.corruptedContent)
                : file.body;
            // ENTITY CORRUPTION EXTENSION POINT:
            // body = SanityTextCorruptor.CorruptText(body, playerInsanity != null ? playerInsanity.insanity : 0f, 1000f);
            loreFileDetailBodyText.text = body;
        }

        if (loreFileDetailSourceText)
            loreFileDetailSourceText.text = $"SOURCE: {file.source.ToString().ToUpper()}";

        if (loreFileDetailCorruptedBadge)
            loreFileDetailCorruptedBadge.gameObject.SetActive(file.isCorrupted);
    }

    public void BackToFileList()
    {
        if (loreFileDetailPanel) loreFileDetailPanel.SetActive(false);
        if (loreFileListPanel) loreFileListPanel.SetActive(true);
        selectedFile = null;
    }

    // HELPERS

    string ApplyTitleCorruption(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        float insanity = playerInsanity != null ? playerInsanity.insanity : 500f;
        return SanityTextCorruptor.CorruptText(input, insanity, 1000f);
    }
}
