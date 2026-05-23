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

        public ComputerCommand(ComputerCommandType type)
        {
            this.type = type;
        }
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

    bool isTransitioning;
    bool isHidden;
    bool videoUnlocked;

    Dictionary<ComputerState,
        Dictionary<ComputerState, List<ComputerCommandType>>> links;

    void Awake()
    {
        BuildGraph();
    }

    void Start()
    {
        state = ComputerState.Off;

        DisableAllScreens();

        if (offScreen)
            offScreen.SetActive(true);

        if (errorOverlay)
            errorOverlay.SetActive(false);

        if (hiddenOverlay)
            hiddenOverlay.SetActive(false);
    }

    void BuildGraph()
    {
        links = new Dictionary<ComputerState,
            Dictionary<ComputerState, List<ComputerCommandType>>>();

        links[ComputerState.Off] =
            new Dictionary<ComputerState, List<ComputerCommandType>>
        {
            {
                ComputerState.MainMenu,
                new List<ComputerCommandType>
                {
                    ComputerCommandType.Enter
                }
            }
        };

        links[ComputerState.MainMenu] =
            new Dictionary<ComputerState, List<ComputerCommandType>>
        {
            {
                ComputerState.SelectionMenu,
                new List<ComputerCommandType>
                {
                    ComputerCommandType.Enter
                }
            }
        };

        links[ComputerState.SelectionMenu] =
            new Dictionary<ComputerState, List<ComputerCommandType>>
        {
            {
                ComputerState.Video,
                new List<ComputerCommandType>
                {
                    ComputerCommandType.Video
                }
            },
            {
                ComputerState.CleanFiles,
                new List<ComputerCommandType>
                {
                    ComputerCommandType.CleanFiles
                }
            },
            {
                ComputerState.ReadUSB,
                new List<ComputerCommandType>
                {
                    ComputerCommandType.ReadUSB
                }
            },
            {
                ComputerState.Save,
                new List<ComputerCommandType>
                {
                    ComputerCommandType.Save
                }
            }
        };
    }

    public void Execute(ComputerCommand cmd)
    {
        if (isTransitioning)
            return;

        Debug.Log($"CMD: {cmd.type} | STATE: {state}");

        // BACK
        if (cmd.type == ComputerCommandType.Back)
        {
            HandleBack();
            return;
        }

        // MAIN MENU → SEARCH
        if (state == ComputerState.MainMenu &&
            cmd.type == ComputerCommandType.Enter)
        {
            StartCoroutine(LoadToSelection());
            return;
        }

        // HIDE ACTION (not a state)
        if (state == ComputerState.SelectionMenu &&
            cmd.type == ComputerCommandType.Hide)
        {
            StartCoroutine(HideRoutine());
            return;
        }

        // VIDEO LOCKED
        if (cmd.type == ComputerCommandType.Video &&
            !videoUnlocked)
        {
            ShowError("ERROR: YOU ARE NOT HIDING");
            return;
        }

        // CLEAN FILES / SAVE LOCKED AFTER HIDING
        if (isHidden &&
            (cmd.type == ComputerCommandType.CleanFiles ||
             cmd.type == ComputerCommandType.Save))
        {
            ShowError("ERROR: YOU ARE HIDING");
            return;
        }

        if (links == null || !links.ContainsKey(state))
            return;

        var options = links[state];

        foreach (var kvp in options)
        {
            if (kvp.Value.Contains(cmd.type))
            {
                SetState(kvp.Key);
                return;
            }
        }

        ShowError("INVALID INPUT");
    }

    void HandleBack()
    {
        Debug.Log("BACK | " + state);

        switch (state)
        {
            case ComputerState.MainMenu:

                // cannot leave while hidden
                if (isHidden)
                {
                    ShowError("ERROR: YOU ARE HIDING");
                    return;
                }

                ExitComputer();
                break;

            case ComputerState.SelectionMenu:
                SetState(ComputerState.MainMenu);
                break;

            case ComputerState.Video:
            case ComputerState.CleanFiles:
            case ComputerState.ReadUSB:
            case ComputerState.Save:
                SetState(ComputerState.SelectionMenu);
                break;
        }
    }

    void SetState(ComputerState newState)
    {
        if (!isTransitioning)
            StartCoroutine(TransitionToState(newState));
    }

    IEnumerator TransitionToState(ComputerState newState)
    {
        isTransitioning = true;

        DisableAllScreens();

        if (loadingScreen)
            loadingScreen.SetActive(true);

        yield return new WaitForSeconds(3f);

        if (loadingScreen)
            loadingScreen.SetActive(false);

        state = newState;

        switch (state)
        {
            case ComputerState.Off:
                offScreen.SetActive(true);
                break;

            case ComputerState.MainMenu:
                menuScreen.SetActive(true);
                break;

            case ComputerState.SelectionMenu:
                selectionScreen.SetActive(true);
                break;

            case ComputerState.Video:
                videoScreen.SetActive(true);
                break;

            case ComputerState.CleanFiles:
                cleanFilesScreen.SetActive(true);
                break;

            case ComputerState.ReadUSB:
                readUSBScreen.SetActive(true);
                break;

            case ComputerState.Save:
                saveScreen.SetActive(true);
                break;
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

        isHidden = true;
        videoUnlocked = true;

        if (player != null && hiddenPoint != null)
        {
            player.position = hiddenPoint.position;
        }
        else
        {
            Debug.LogWarning("Hidden point not assigned");
        }

        if (hiddenOverlay)
            hiddenOverlay.SetActive(true);

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
        if (state != ComputerState.Off)
            return;
        DisableAllScreens();

        StartCoroutine(
            TransitionToState(
                ComputerState.MainMenu
            )
        );
    }

    void ExitComputer()
    {
        Debug.Log("Exit Computer");

        SimpleFPSController playerController =
            FindObjectOfType<SimpleFPSController>();

        if (playerController != null)
        {
            playerController.ExitComputerMode();
        }
    }

    Coroutine errorRoutine;

    void ShowError(string message)
    {
        Debug.LogWarning(message);

        if (errorOverlay == null || errorText == null)
            return;

        if (errorRoutine != null)
            StopCoroutine(errorRoutine);

        errorOverlay.SetActive(true);
        errorText.text = message;

        errorRoutine = StartCoroutine(HideErrorRoutine());
    }

    IEnumerator HideErrorRoutine()
    {
        yield return new WaitForSeconds(2f);

        if (errorOverlay != null)
            errorOverlay.SetActive(false);

        errorRoutine = null;
    }
}