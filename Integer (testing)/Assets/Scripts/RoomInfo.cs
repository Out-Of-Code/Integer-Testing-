using UnityEngine;
using System.Collections.Generic;
public class RoomInstance : MonoBehaviour
{
    [System.Serializable]
    public class ExitData
    {
        public Transform exitPoint;
        public DoorSlot doorSlot;
    }
    public List<FurnitureSpawn> furnitureSpawns = new();

    public List<Collider> spawnedFurnitureColliders = new();

    public bool generatedFurniture;
    
    public List<Transform> entries = new();
    public List<ExitData> exits = new();
    
    public List<ExitData> unusedExits = new();

    [HideInInspector]
    public ExitData chosenExit;

    void Awake()
    {
        furnitureSpawns.Clear();

        furnitureSpawns.AddRange(
            GetComponentsInChildren<FurnitureSpawn>());

        entries.Clear();
        exits.Clear();

        Transform entryRoot =
            transform.Find("EntryPoints");

        Transform exitRoot =
            transform.Find("ExitPoints");

        if (entryRoot != null)
        {
            foreach (Transform t in entryRoot)
            {
                entries.Add(t);
            }
        }

        if (exitRoot != null)
        {
            foreach (Transform t in exitRoot)
            {
                ExitData data = new ExitData();

                data.exitPoint = t;

                data.doorSlot =
                    t.GetComponentInChildren<DoorSlot>();

                exits.Add(data);
            }
        }
    }

    // =====================================================
    // OVERLAP CHECK
    // =====================================================

    bool CanPlace(Transform prefabRoot)
    {
        Collider[] colliders =
            prefabRoot.GetComponentsInChildren<Collider>();

        foreach (var col in colliders)
        {
            foreach (var existing in spawnedFurnitureColliders)
            {
                if (col.bounds.Intersects(existing.bounds))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // =====================================================
    // GENERATE
    // =====================================================

    public void GenerateFurniture(WeightedFurniture[] furniturePool)
    {
        if (generatedFurniture)
            return;

        foreach (FurnitureSpawn slot in furnitureSpawns)
        {
            TrySpawnFurniture(slot, furniturePool);
        }

        generatedFurniture = true;
    }

    // =====================================================
    // SLOT SPAWN LOGIC
    // =====================================================

    void TrySpawnFurniture(
        FurnitureSpawn slot,
        WeightedFurniture[] furniturePool)
    {
        // chance check (skip only optional slots)
        if (!slot.required)
        {
            if (Random.value > slot.spawnChance)
                return;
        }

        List<WeightedFurniture> valid = new();

        foreach (var furniture in furniturePool)
        {
            bool matchesType =
                slot.allowedType == FurnitureType.Any ||
                furniture.type == slot.allowedType;

            if (matchesType)
                valid.Add(furniture);
        }

        // nothing fits this slot
        if (valid.Count == 0)
        {
            if (slot.required)
            {
                Debug.LogWarning(
                    $"Required furniture missing for slot in {name}");
            }

            return;
        }

        // =====================================================
        // TRY MULTIPLE OPTIONS BEFORE FAILING SLOT
        // =====================================================

        for (int i = 0; i < 3; i++)
        {
            WeightedFurniture chosen = Pick(valid);

            GameObject spawned =
                Instantiate(
                    chosen.prefab,
                    slot.transform.position,
                    slot.transform.rotation,
                    slot.transform);

            // IMPORTANT: validate BEFORE committing
            if (CanPlace(spawned.transform))
            {
                slot.occupied = true;

                Collider[] cols =
                    spawned.GetComponentsInChildren<Collider>();

                spawnedFurnitureColliders.AddRange(cols);

                return;
            }

            Destroy(spawned);
        }

        // if we reach here → slot failed to place anything
    }

    // =====================================================
    // WEIGHTED PICK
    // =====================================================

    WeightedFurniture Pick(List<WeightedFurniture> list)
    {
        int total = 0;

        foreach (var item in list)
            total += item.weight;

        int rand = Random.Range(0, total);

        foreach (var item in list)
        {
            rand -= item.weight;

            if (rand < 0)
                return item;
        }

        return list[0];
    }
}