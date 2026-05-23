using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum moveDirection
{
    None,
    Left,
    Right,
    Up,
    Down,
    UTurn
}

public enum FacingDirection
{
    North,
    East,
    South,
    West
}

public enum FurnitureType
{
    Any,
    Desk,
    Shelf,
    Cabinet,
    Table
}
[System.Serializable]
public class WeightedRoom
{
    public GameObject room;

    [Min(1)]
    public int weight = 1;

    public bool canRepeat = true;

    public moveDirection direction;

    [Tooltip("-1 = normal spawning")]
    public int spawnAtRoom = -1;

    public float heightChange;
}
[System.Serializable]
public class WeightedFurniture
{
    public GameObject prefab;

    public int weight = 1;

    public FurnitureType type;
}
[System.Serializable]
public class GameSettings
{
    public bool useSeededRun;
    public int seed;

    public int roomsPerChunk = 100;
    public int operationsPerFrame = 25;
    public float cooldown = 0f;

    public bool debugDisableRules;
    public bool allowCollisions;

    public bool autoOpenDoors;
    public bool enableInteractHighlight;
}
class GenerationNode
{
    public List<WeightedRoom> triedRooms = new();
}

public class RoomGenerator : MonoBehaviour
{
    // =====================================================
    // INSPECTOR
    // =====================================================

    [Header("Rooms")]
    public WeightedRoom[] rooms;
    public WeightedRoom[] sideRooms;
    
    [Header("Doors")]
    [Tooltip("lmao rooms & doors")]
    public GameObject regularDoorPrefab;
    public GameObject SideRoomDoorPrefab;
    public GameObject[] blockedDoorPrefabs;
    
    public WeightedFurniture[] Furniture;

    public GameObject PlayerPrefab;
    [Header("Chunk Generation")]
    public int roomsPerChunk = 100;

    [Header("Performance")]
    public int operationsPerFrame = 25;

    [Tooltip("Optional slowdown between frames")]
    public float cooldown = 0f;

    [Header("Debug")]
    public bool verboseLogging = false;

    public bool debugDisableAllRules = false;

    public bool debugAllowCollisions = false;
    
    [Header("Seed Debug")]
    public bool useSeededRun;
    public int debugSeed;
    
    public GameSettings settings;
    
    [ContextMenu("Generate With Seed")]
    void GenerateWithSeed()
    {
        StartSeededRun(settings.seed);
    }
    
    public int playerRoomIndex = 0;

    // =====================================================
    // INTERNAL
    // =====================================================
    public void SetPlayerRoom(int index)
    {
        playerRoomIndex = Mathf.Clamp(index, 0, roomHistory.Count - 1);
    }

    public GameObject GetPlayerRoom()
    {
        return roomHistory.Count > playerRoomIndex
            ? roomHistory[playerRoomIndex]
            : null;
    }
    class PlacedRoom
    {
        public GameObject obj;

        public WeightedRoom data;

        public List<Bounds> bounds;
    }

    readonly List<GameObject> roomHistory = new();

    readonly List<PlacedRoom> placedRooms = new();

    readonly Stack<GenerationNode> stack = new();

    Coroutine generationRoutine;

    Transform lastExit;

    WeightedRoom lastRoomData;

    int heightBalance;

    FacingDirection currentFacing =
        FacingDirection.North;

    FacingDirection desiredFacing =
        FacingDirection.North;

    bool isGenerating;

    bool isBacktracking;

    bool rulesOriginallyDisabled;

    int targetRoomCount;
    
    int globalRoomIndex;
    
    public int currentSeed;

    public SPRINTController nodeController;
    private bool hasGenerated;
    private GameObject door;

    // =====================================================
    // UNITY
    // =====================================================

    void Start()
    {
        if (UIManager.Instance != null)
            settings = UIManager.Instance.settings;
        targetRoomCount = settings.roomsPerChunk;

        if (nodeController == null)
            nodeController = FindObjectOfType<SPRINTController>();
    }
    public void StartGeneration()
    {
        int seedToUse;

        if (settings.useSeededRun)
            seedToUse = settings.seed;
        else
            seedToUse = Random.Range(
                int.MinValue,
                int.MaxValue);

        RunState.Init(seedToUse, settings.useSeededRun);

        currentSeed = RunState.Seed;

        stack.Clear();
        stack.Push(new GenerationNode());

        GenerateNextChunk();
    }
    // =====================================================
    // PUBLIC API
    // =====================================================

    public void GenerateNextChunk()
    {
        targetRoomCount =
            roomHistory.Count + settings.roomsPerChunk;

        if (generationRoutine != null)
            StopCoroutine(generationRoutine);

        generationRoutine =
            StartCoroutine(GenerationLoop());
    }

    public void ClearOldRoomsKeepLast()
    {
        if (roomHistory.Count <= 1)
            return;

        GameObject lastRoom =
            roomHistory[^1];

        // destroy old rooms

        for (int i = 0; i < roomHistory.Count - 1; i++)
        {
            if (roomHistory[i] != null)
                Destroy(roomHistory[i]);
        }

        roomHistory.Clear();
        placedRooms.Clear();
        stack.Clear();

        roomHistory.Add(lastRoom);

        WeightedRoom data =
            FindRoom(lastRoom);

        if (data == null)
        {
            Debug.LogError(
                "Failed rebuilding state from last room.");

            return;
        }

        RoomBounds rb =
            lastRoom.GetComponent<RoomBounds>();

        placedRooms.Add(new PlacedRoom
        {
            obj = lastRoom,
            data = data,
            bounds = rb.GetWorldBounds()
        });

        // rebuild state

        lastRoomData = data;

        lastExit =
            lastRoom.transform.Find("Exit");

        heightBalance =
            Mathf.RoundToInt(
                data.heightChange);

        currentFacing =
            FacingDirection.North;

        ApplyDirection(data.direction);

        Log("OLD ROOMS CLEARED");
    }

    public void HardReset()
    {
        if (generationRoutine != null)
            StopCoroutine(generationRoutine);

        foreach (var obj in roomHistory)
        {
            if (obj != null)
                Destroy(obj);
        }

        roomHistory.Clear();

        placedRooms.Clear();

        stack.Clear();

        lastExit = null;

        lastRoomData = null;

        heightBalance = 0;

        currentFacing =
            FacingDirection.North;

        isBacktracking = false;

        settings.debugDisableRules =
            rulesOriginallyDisabled;

        targetRoomCount = settings.roomsPerChunk;

        generationRoutine =
            StartCoroutine(GenerationLoop());
        
        nodeController.ResetState();

        Log("HARD RESET");
    }

    // =====================================================
    // GENERATION LOOP
    // =====================================================

    IEnumerator GenerationLoop()
    {
        isGenerating = true;

        while (isGenerating)
        {
            for (int i = 0;
                 i < settings.operationsPerFrame;
                 i++)
            {
                if (roomHistory.Count >=
                    targetRoomCount)
                {
                    FinishGeneration();

                    yield break;
                }

                bool success =
                    TryPlaceWithMemory();

                if (!success)
                {
                    bool backtracked =
                        Backtrack();

                    if (!backtracked)
                    {
                        Debug.LogWarning(
                            "Generation completely failed.");

                        HardReset();

                        yield break;
                    }
                }
            }

            RunState.Value();
            yield return null;

            if (settings.cooldown > 0f)
            {
                yield return
                    new WaitForSeconds(settings.cooldown);
            }
        }

        FinishGeneration();
    }

    void FinishGeneration()
    {
        isGenerating = false;

        if (isBacktracking)
        {
            isBacktracking = false;

            settings.debugDisableRules =
                rulesOriginallyDisabled;
        }

        if (!hasGenerated)
        {
            hasGenerated = true;

            GameObject player =
                Instantiate(PlayerPrefab);

            GameObject firstRoom =
                roomHistory.Count > 0
                    ? roomHistory[0]
                    : null;

            if (firstRoom != null)
            {
                player.transform.position =
                    firstRoom.transform.position +
                    Vector3.up * 2f;
            }
        }

        Log("GENERATION COMPLETE");
    }

    // =====================================================
    // TRY PLACE
    // =====================================================

    bool TryPlaceWithMemory()
    {
        foreach (var room in rooms)
        {
            if (room.spawnAtRoom == roomHistory.Count + 1)
            {
                // ensure it is not accidentally weighted elsewhere
                return TryPlace(room);
            }
        }

        GenerationNode node =
            stack.Count > 0 ? stack.Peek() : null;

        List<WeightedRoom> options = GetValidRooms();

        if (node != null)
        {
            options.RemoveAll(r =>
                node.triedRooms.Contains(r));
        }

        if (options.Count == 0)
            return false;

        WeightedRoom chosen = Pick(options);

        if (node != null)
            node.triedRooms.Add(chosen);

        bool success = TryPlace(chosen);

        if (!success)
            return false;

        stack.Push(new GenerationNode());

        return true;
    }

    // =====================================================
    // PLACE ROOM
    // =====================================================

    bool TryPlace(WeightedRoom data)
    {
        GameObject obj =
            Instantiate(data.room);

        RoomInstance roomInstance =
            obj.GetComponent<RoomInstance>();

        if (roomInstance == null)
        {
            Debug.LogError(
                $"Missing RoomInstance on {data.room.name}");

            Destroy(obj);

            return false;
        }

        if (roomInstance.entries.Count == 0 ||
            roomInstance.exits.Count == 0)
        {
            Debug.LogError(
                $"Room has no entries/exits: {data.room.name}");

            Destroy(obj);

            return false;
        }

        Transform entry =
            roomInstance.entries[0];

        RoomInstance.ExitData exit =
            roomInstance.exits[
                Random.Range(0, roomInstance.exits.Count)];

        roomInstance.chosenExit = exit;
        roomInstance.unusedExits.Clear();

        foreach (RoomInstance.ExitData e in roomInstance.exits)
        {
            if (e != exit)
            {
                roomInstance.unusedExits.Add(e);
            }
        }

        if (entry == null || exit.exitPoint == null)
        {
            Debug.LogError(
                $"Missing Entry/Exit on {data.room.name}");

            Destroy(obj);

            return false;
        }

        // align room

        if (lastExit == null)
        {
            obj.transform.position =
                Vector3.zero;
        }
        else
        {
            obj.transform.rotation =
                lastExit.rotation *
                Quaternion.Inverse(
                    entry.localRotation);

            obj.transform.position +=
                lastExit.position -
                entry.position;
        }

        Physics.SyncTransforms();

        // validate

        if (!Validate(obj))
        {
            Destroy(obj);

            return false;
        }

        Commit(obj, data, roomInstance);

        return true;
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    bool Validate(GameObject obj)
    {
        if (settings.allowCollisions)
            return true;

        RoomBounds rb =
            obj.GetComponent<RoomBounds>();

        if (rb == null)
        {
            Debug.LogError(
                $"No RoomBounds on {obj.name}");

            return false;
        }

        List<Bounds> newBounds =
            rb.GetWorldBounds();

        foreach (var placed in placedRooms)
        {
            foreach (var a in newBounds)
            {
                foreach (var b in placed.bounds)
                {
                    if (a.Intersects(b))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    // =====================================================
    // COMMIT
    // =====================================================

void Commit(
    GameObject obj,
    WeightedRoom data,
    RoomInstance roomInstance)
{
    roomHistory.Add(obj);

    globalRoomIndex++;

    // =====================================================
    // CORE STATE UPDATE
    // =====================================================

    lastExit = roomInstance.chosenExit.exitPoint;
    lastRoomData = data;

    ApplyDirection(data.direction);

    heightBalance += Mathf.RoundToInt(data.heightChange);

    RoomBounds rb =
        obj.GetComponent<RoomBounds>();

    placedRooms.Add(new PlacedRoom
    {
        obj = obj,
        data = data,
        bounds = rb.GetWorldBounds()
    });

    // =====================================================
    // SIDE ROOMS (they consume exits)
    // =====================================================

    HashSet<RoomInstance.ExitData> usedExits = new();

    GenerateSideRooms(obj, roomInstance, usedExits);

    usedExits.Add(roomInstance.chosenExit);

    // =====================================================
    // MAIN PATH DOOR
    // =====================================================

    if (roomInstance.chosenExit.doorSlot != null)
    {
        door = Instantiate(
            regularDoorPrefab,
            roomInstance.chosenExit.doorSlot.transform.position,
            roomInstance.chosenExit.doorSlot.transform.rotation,
            roomInstance.chosenExit.doorSlot.transform);
        door.GetComponentInParent<Door>().roomIndex = globalRoomIndex;
    }

    usedExits.Add(roomInstance.chosenExit);

    // =====================================================
    // BLOCKED DOORS (only truly unused exits)
    // =====================================================

    foreach (RoomInstance.ExitData exit in roomInstance.exits)
    {
        if (exit.doorSlot == null)
            continue;

        if (usedExits.Contains(exit))
            continue;

        GameObject prefab =
            blockedDoorPrefabs[
                Random.Range(0, blockedDoorPrefabs.Length)];

        Instantiate(
            prefab,
            exit.doorSlot.transform.position,
            exit.doorSlot.transform.rotation,
            exit.doorSlot.transform);
    }

    // =====================================================
    // FURNITURE
    // =====================================================

    if (roomInstance != null)
    {
        roomInstance.GenerateFurniture(Furniture);
    }

    // =====================================================
    // LABELS
    // =====================================================

    DoorLabel[] labels =
        obj.GetComponentsInChildren<DoorLabel>();

    foreach (DoorLabel label in labels)
    {
        label.SetNumber(roomHistory.Count);
    }

    // =====================================================
    // CONTROLLER HOOK
    // =====================================================

    if (nodeController != null)
    {
        nodeController.OnRoomGenerated(globalRoomIndex);
    }

    // =====================================================
    // DOOR STATE TAGGING
    // =====================================================

    Door[] doors = obj.GetComponentsInChildren<Door>();

    foreach (var d in doors)
    {
        d.roomIndex = globalRoomIndex;
    }
}
void GenerateSideRooms(
    GameObject parentObj,
    RoomInstance parentRoom,
    HashSet<RoomInstance.ExitData> usedExits)
{
    foreach (RoomInstance.ExitData exit in parentRoom.unusedExits)
    {
        if (exit.exitPoint == null)
            continue;

        // already used by main path or another side room
        if (usedExits.Contains(exit))
            continue;

        // chance gate
        if (Random.value > 0.35f)
            continue;

        if (sideRooms == null || sideRooms.Length == 0)
            return;
        WeightedRoom chosen =
            sideRooms[Random.Range(0, sideRooms.Length)];

        GameObject obj =
            Instantiate(chosen.room);
        obj.transform.SetParent(parentObj.transform);
        RoomInstance sideInstance =
            obj.GetComponent<RoomInstance>();

        if (sideInstance == null || sideInstance.entries.Count == 0)
        {
            Destroy(obj);
            continue;
        }

        Transform entry = sideInstance.entries[0];

        // align to exit
        obj.transform.rotation =
            exit.exitPoint.rotation *
            Quaternion.Inverse(entry.localRotation);

        obj.transform.position +=
            exit.exitPoint.position -
            entry.position;

        Physics.SyncTransforms();

        // collision check
        if (!Validate(obj))
        {
            Destroy(obj);
            continue;
        }

        // mark this exit as consumed
        usedExits.Add(exit);

        // register placement (IMPORTANT: reuse same system)
        RoomBounds rb = obj.GetComponent<RoomBounds>();

        placedRooms.Add(new PlacedRoom
        {
            obj = obj,
            data = chosen,
            bounds = rb.GetWorldBounds()
        });

        // furniture
        sideInstance.GenerateFurniture(Furniture);

        // side doors (always blocked/openable separately)
        foreach (var sideExit in sideInstance.exits)
        {
            if (sideExit.doorSlot == null)
                continue;

            Instantiate(
                SideRoomDoorPrefab,
                sideExit.doorSlot.transform.position,
                sideExit.doorSlot.transform.rotation,
                sideExit.doorSlot.transform);
        }
    }
}
    public void OnDoorOpened(int roomIndex)
    {
        playerRoomIndex = Mathf.Max(playerRoomIndex, roomIndex);
    }

    // =====================================================
    // BACKTRACK
    // =====================================================

    bool Backtrack()
    {
        if (!isBacktracking)
        {
            isBacktracking = true;

            rulesOriginallyDisabled =
                settings.debugDisableRules;

            // temporarily disable rules
            settings.debugDisableRules = true;
        }

        while (stack.Count > 0)
        {
            stack.Pop();

            if (roomHistory.Count > 0)
            {
                GameObject obj = roomHistory[^1];

                roomHistory.RemoveAt(roomHistory.Count - 1);

                placedRooms.RemoveAll(p => p.obj == obj);

                Destroy(obj);
                ReindexRooms();
            }

            RebuildState();

            if (stack.Count <= 0)
                continue;

            GenerationNode parent = stack.Peek();

            List<WeightedRoom> remaining = GetValidRooms();

            remaining.RemoveAll(r =>
                parent.triedRooms.Contains(r));

            if (remaining.Count > 0)
                return true;
        }

        return false;
    }

    // =====================================================
    // REBUILD
    // =====================================================

    void RebuildState()
    {
        lastExit = null;
        lastRoomData = null;
        heightBalance = 0;
        currentFacing = FacingDirection.North;

        for (int i = 0; i < roomHistory.Count; i++)
        {
            GameObject obj = roomHistory[i];

            WeightedRoom data = FindRoom(obj);
            if (data == null) continue;

            lastRoomData = data;
            RoomInstance roomInstance =
                obj.GetComponent<RoomInstance>();

            if (roomInstance != null)
            {
                lastExit = roomInstance.chosenExit.exitPoint;
            }

            heightBalance += Mathf.RoundToInt(data.heightChange);

            ApplyDirection(data.direction);
        }
    }

    // =====================================================
    // VALID ROOMS
    // =====================================================
    public GameObject GetRoomAtIndex(int index)
    {
        if (index < 0 || index >= roomHistory.Count)
            return null;

        return roomHistory[index];
    }
    List<WeightedRoom> GetValidRooms()
    {
        
        List<WeightedRoom> valid = new();

        foreach (var room in rooms)
        {
            // forced room only

            if (room.spawnAtRoom > -1)
            {
                if (room.spawnAtRoom ==
                    roomHistory.Count + 1)
                {
                    valid.Add(room);
                }

                continue;
            }

            if (!settings.debugDisableRules)
            {
                // no repeat

                if (!room.canRepeat &&
                    lastRoomData != null &&
                    room.room ==
                    lastRoomData.room)
                {
                    continue;
                }

                // orientation recovery

                if (currentFacing !=
                    desiredFacing)
                {
                    if (currentFacing ==
                        FacingDirection.West &&
                        room.direction ==
                        moveDirection.Left)
                    {
                        continue;
                    }

                    if (currentFacing ==
                        FacingDirection.East &&
                        room.direction ==
                        moveDirection.Right)
                    {
                        continue;
                    }
                }

                // height balancing

                if (heightBalance > 0 &&
                    room.heightChange > 0)
                {
                    continue;
                }

                if (heightBalance < 0 &&
                    room.heightChange < 0)
                {
                    continue;
                }
            }

            valid.Add(room);

            // slight UTurn preference

            if (currentFacing ==
                FacingDirection.South &&
                room.direction ==
                moveDirection.UTurn)
            {
                valid.Add(room);
            }
        }

        return valid;
    }

    // =====================================================
    // PICK
    // =====================================================

    WeightedRoom Pick(
        List<WeightedRoom> list)
    {
        int total = 0;

        foreach (var r in list)
            total += r.weight;

        int rand = RunState.Range(0, total);

        foreach (var r in list)
        {
            rand -= r.weight;

            if (rand < 0)
                return r;
        }

        return list[0];
    }

    // =====================================================
    // UTIL
    // =====================================================

    WeightedRoom FindRoom(GameObject obj)
    {
        string clean =
            obj.name
            .Replace("(Clone)", "")
            .Trim();

        foreach (var r in rooms)
        {
            if (r.room.name == clean)
                return r;
        }

        return null;
    }

    void ApplyDirection(
        moveDirection direction)
    {
        switch (direction)
        {
            case moveDirection.Left:
                currentFacing =
                    RotateLeft(currentFacing);
                break;

            case moveDirection.Right:
                currentFacing =
                    RotateRight(currentFacing);
                break;

            case moveDirection.UTurn:
                currentFacing =
                    RotateUTurn(currentFacing);
                break;
        }
    }

    FacingDirection RotateLeft(
        FacingDirection dir)
    {
        switch (dir)
        {
            case FacingDirection.North:
                return FacingDirection.West;

            case FacingDirection.West:
                return FacingDirection.South;

            case FacingDirection.South:
                return FacingDirection.East;

            default:
                return FacingDirection.North;
        }
    }

    FacingDirection RotateRight(
        FacingDirection dir)
    {
        switch (dir)
        {
            case FacingDirection.North:
                return FacingDirection.East;

            case FacingDirection.East:
                return FacingDirection.South;

            case FacingDirection.South:
                return FacingDirection.West;

            default:
                return FacingDirection.North;
        }
    }

    FacingDirection RotateUTurn(
        FacingDirection dir)
    {
        switch (dir)
        {
            case FacingDirection.North:
                return FacingDirection.South;

            case FacingDirection.South:
                return FacingDirection.North;

            case FacingDirection.East:
                return FacingDirection.West;

            default:
                return FacingDirection.East;
        }
    }

    void Log(string msg)
    {
        if (!verboseLogging)
            return;

        Debug.Log(msg);
    }

    // =====================================================
    // INFO
    // =====================================================

    public bool IsGenerating()
    {
        return isGenerating;
    }

    public int GetRoomCount()
    {
        return roomHistory.Count;
    }

    public GameObject GetLastRoom()
    {
        if (roomHistory.Count == 0)
            return null;

        return roomHistory[^1];
    }
    public void StartSeededRun(int seed)
    {
        settings.useSeededRun = true;

        RunState.Init(seed, true);

        currentSeed = seed;

        HardReset();

        GenerateNextChunk();
    }
    public int GetCurrentSeed()
    {
        return RunState.Seed;
    }
    public string GetSeedString()
    {
        return RunState.Seed.ToString();
    }
    void ReindexRooms()
    {
        globalRoomIndex = 0;

        foreach (GameObject obj in roomHistory)
        {
            globalRoomIndex++;

            Door[] doors = obj.GetComponentsInChildren<Door>();

            foreach (var d in doors)
            {
                d.roomIndex = globalRoomIndex;
            }
        }
    }
}