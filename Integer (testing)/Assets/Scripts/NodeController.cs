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

    void Start()
    {
        if (generator == null)
        {
            generator = FindObjectOfType<RoomGenerator>();
        }
    }
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= triggerTime)
        {
            SpawnSPRINT();
            timer = 0f;
        }
    }

    public void OnDoorOpened()
    {
        timer += doorAcceleration;
    }
    void SpawnSPRINT()
    {
        if (activeEntity != null)
            Destroy(activeEntity);

        int spawnIndex =
            Mathf.Max(0, generator.GetRoomCount() - 4);

        GameObject spawnRoom =
            generator.GetRoomAtIndex(spawnIndex);

        if (spawnRoom == null)
            return;

        Transform spawnPoint =
            spawnRoom.transform.Find("Entry");

        activeEntity =
            Instantiate(
                sprintPrefab,
                spawnPoint.position,
                spawnPoint.rotation);
    }
    public void OnRoomGenerated(int index)
    {
        // later: update node map, spawn logic, etc.
    }
}