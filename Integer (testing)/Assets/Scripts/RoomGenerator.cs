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
    
    [Header("Doors")]
    [Tooltip("lmao rooms & doors")]
    public GameObject regularDoorPrefab;
    
    public WeightedFurniture[] Furniture;

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
    
    [ContextMenu("Generate With Seed")]
    void GenerateWithSeed()
    {
        StartSeededRun(debugSeed);
    }

    // =====================================================
    // INTERNAL
    // =====================================================

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

    // =====================================================
    // UNITY
    // =====================================================

    void Start()
    {
        int seedToUse;

        if (useSeededRun)
            seedToUse = debugSeed;
        else
            seedToUse = Random.Range(
                int.MinValue,
                int.MaxValue);

        RunState.Init(seedToUse, useSeededRun);

        currentSeed = RunState.Seed;

        stack.Push(new GenerationNode());

        GenerateNextChunk();
    }
    // =====================================================
    // PUBLIC API
    // =====================================================

    public void GenerateNextChunk()
    {
        targetRoomCount =
            roomHistory.Count + roomsPerChunk;

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

        debugDisableAllRules =
            rulesOriginallyDisabled;

        targetRoomCount = roomsPerChunk;

        generationRoutine =
            StartCoroutine(GenerationLoop());

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
                 i < operationsPerFrame;
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

            if (cooldown > 0f)
            {
                yield return
                    new WaitForSeconds(cooldown);
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

            debugDisableAllRules =
                rulesOriginallyDisabled;
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

        Transform entry =
            obj.transform.Find("Entry");

        Transform exit =
            obj.transform.Find("Exit");

        if (entry == null || exit == null)
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

        Commit(obj, data, exit);

        return true;
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    bool Validate(GameObject obj)
    {
        if (debugAllowCollisions)
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
        Transform exit)
    {
        roomHistory.Add(obj);
        globalRoomIndex++;

        lastExit = exit;

        lastRoomData = data;

        ApplyDirection(data.direction);

        heightBalance +=
            Mathf.RoundToInt(
                data.heightChange);

        RoomBounds rb =
            obj.GetComponent<RoomBounds>();

        placedRooms.Add(new PlacedRoom
        {
            obj = obj,
            data = data,
            bounds = rb.GetWorldBounds()
        });
        DoorSlot slot =
            obj.GetComponentInChildren<DoorSlot>();

        if (slot != null)
        {
            Instantiate(
                regularDoorPrefab,
                slot.transform.position,
                slot.transform.rotation,
                slot.transform);
        }
        RoomInstance roomInstance =
            obj.GetComponent<RoomInstance>();

        if (roomInstance != null)
        {
            roomInstance.GenerateFurniture(
                Furniture);
        }
        DoorLabel label =
            obj.GetComponentInChildren<DoorLabel>();

        if (label != null)
        {
            // first room = 001, next = 002, etc.
            label.SetNumber(roomHistory.Count);
        }
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
                debugDisableAllRules;

            // temporarily disable rules
            debugDisableAllRules = true;
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
            lastExit = obj.transform.Find("Exit");

            heightBalance += Mathf.RoundToInt(data.heightChange);

            ApplyDirection(data.direction);
        }
    }

    // =====================================================
    // VALID ROOMS
    // =====================================================

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

            if (!debugDisableAllRules)
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
        useSeededRun = true;

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
}