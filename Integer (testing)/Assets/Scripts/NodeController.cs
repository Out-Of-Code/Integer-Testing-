using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SPRINTController : MonoBehaviour
{
    public float timer;
    public float triggerTime = 120f;

    public int doorAcceleration = 2;

    public RoomGenerator generator;

    public GameObject sprintPrefab;

    private GameObject activeEntity;
    
    private int lastKnownPlayerRoom = 0;
    public Transform lastPlayerRoom;
    public float pressure;
    
    bool active;
    
    public int lastOpenedRoomIndex = 0;

    void Start()
    {
        if (generator == null)
        {
            generator = FindObjectOfType<RoomGenerator>();
        }
    }
    void Update()
    {
        pressure += Time.deltaTime;

        if (pressure >= triggerTime)
        {
            SpawnSPRINT();
            pressure = 0f;
        }
        
    }

    public void OnDoorOpened(int roomIndex)
    {
        Debug.Log("SPR.INT updated room to " + roomIndex);

        lastKnownPlayerRoom = roomIndex;

        pressure += doorAcceleration;

        lastPlayerRoom =
            generator.GetRoomAtIndex(roomIndex)?.transform;
    }
    void SpawnSPRINT()
    {
        Debug.Log("Spawning SPRINT");
        if (generator == null)
            return;

        int spawnIndex = Mathf.Max(0, lastKnownPlayerRoom - 4);

        GameObject spawnRoom = generator.GetRoomAtIndex(spawnIndex);

        if (spawnRoom == null)
        {
            spawnRoom = generator.GetRoomAtIndex(lastKnownPlayerRoom);
            Debug.LogWarning("No spawn room found for " + spawnIndex);
        }

        Transform spawnPoint = spawnRoom.transform.Find("EntryPoints").Find("Entry").transform;

        if (spawnPoint == null)
        {
            Debug.LogWarning("No spawn point found for " + spawnIndex);
            return;
        }

        if (activeEntity != null)
        {
            Debug.LogWarning("Already active, deleting active entity");
            Destroy(activeEntity);
        }

        activeEntity = Instantiate(
            sprintPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        SPR_INT sprint =
            activeEntity.GetComponent<SPR_INT>();

        if (sprint != null)
        {
            List<RoomInstance> rooms =
                new List<RoomInstance>();

            int endIndex = generator.GetRoomCount() - 1;

            for (int i = spawnIndex; i <= endIndex; i++)
            {
                GameObject roomObj =
                    generator.GetRoomAtIndex(i);

                if (roomObj == null)
                    continue;

                RoomInstance instance =
                    roomObj.GetComponent<RoomInstance>();

                if (instance != null)
                {
                    rooms.Add(instance);
                }
            }
            sprint.BuildRoute(rooms);
        }
        if (activeEntity != null)
        {
            Debug.Log("spawned ", activeEntity);
        }
    }
    public void OnRoomGenerated(int index)
    {
        // later: update node map, spawn logic, etc.
    }
    public void ResetState()
    {
        pressure = 0f;
        lastKnownPlayerRoom = 0;

        if (activeEntity != null)
        {
            Destroy(activeEntity);
            activeEntity = null;
        }
    }
}