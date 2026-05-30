using UnityEngine;

public class ComputerInputController : MonoBehaviour
{
    public ComputerController computer;

    SimpleFPSController player;
    Camera mainCam;
    Canvas canvas;

    void Start()
    {
        player = FindObjectOfType<SimpleFPSController>();
        canvas = FindObjectOfType<Canvas>();
    }

    void Update()
    {
        // player spawns later
        if (player == null)
        {
            player = FindObjectOfType<SimpleFPSController>();

            if (player == null)
                return;
        }

        // camera gets recreated
        if (mainCam == null)
        {
            mainCam = Camera.main;

            if (mainCam != null &&
                canvas != null)
            {
                canvas.worldCamera = mainCam;
            }
        }

        if (!computer)
            return;

        if (!player.inComputer ||
            player.currentComputer != computer)
            return;

        // ENTER / BACK
    if (Input.GetKeyDown(KeyCode.Y) ||
        Input.GetKeyDown(KeyCode.Return))
    {
        computer.Execute(
            new ComputerController.ComputerCommand(
                ComputerController.ComputerCommandType.Enter));
    }

    if (Input.GetKeyDown(KeyCode.N) ||
        Input.GetKeyDown(KeyCode.Backspace) ||
        Input.GetKeyDown(KeyCode.X))
    {
        computer.Execute(
            new ComputerController.ComputerCommand(
                ComputerController.ComputerCommandType.Back));
    }

    // ---------- Selection Screen ----------
    if (computer.state ==
        ComputerController.ComputerState.SelectionMenu)
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            computer.Execute(
                new ComputerController.ComputerCommand(
                    ComputerController.ComputerCommandType.Hide));
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            computer.Execute(
                new ComputerController.ComputerCommand(
                    ComputerController.ComputerCommandType.ReadUSB));
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            computer.Execute(
                new ComputerController.ComputerCommand(
                    ComputerController.ComputerCommandType.CleanFiles));
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            computer.Execute(
                new ComputerController.ComputerCommand(
                    ComputerController.ComputerCommandType.Video));
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            computer.Execute(
                new ComputerController.ComputerCommand(
                    ComputerController.ComputerCommandType.Save));
        }
    }

    // ---------- Read USB ----------
    else if (computer.state ==
             ComputerController.ComputerState.ReadUSB)
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            computer.OpenUSB(0);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            computer.OpenUSB(1);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            computer.OpenUSB(2);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            computer.OpenUSB(3);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            computer.OpenUSB(4);
    }
}
}