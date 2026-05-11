using System.Collections.Generic;
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
    Transform lastPlayerRoom;
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
        lastKnownPlayerRoom = roomIndex;
        pressure += doorAcceleration;

        lastPlayerRoom = generator.GetRoomAtIndex(roomIndex)?.transform;
    }
    void SpawnSPRINT()
    {
        if (generator == null)
            return;

        int spawnIndex = Mathf.Max(0, lastKnownPlayerRoom - 4);

        GameObject spawnRoom = generator.GetRoomAtIndex(spawnIndex);

        if (spawnRoom == null)
            return;

        Transform spawnPoint = spawnRoom.transform.Find("Entry");

        if (spawnPoint == null)
            return;

        if (activeEntity != null)
            Destroy(activeEntity);

        activeEntity = Instantiate(
            sprintPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );
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