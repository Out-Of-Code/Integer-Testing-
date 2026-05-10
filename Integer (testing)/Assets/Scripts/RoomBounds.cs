using System.Collections.Generic;
using UnityEngine;

public class RoomBounds : MonoBehaviour
{
    public List<Collider> boundsColliders = new List<Collider>();

    void Awake()
    {
        boundsColliders.Clear();

        foreach (var col in GetComponentsInChildren<Collider>())
        {
            // ONLY include hitbox colliders
            if (col.gameObject.name.Contains("Hitbox"))
            {
                boundsColliders.Add(col);

                // IMPORTANT: prevent physics interference
                col.isTrigger = true;
            }
        }
    }

    public List<Bounds> GetWorldBounds()
    {
        List<Bounds> result = new List<Bounds>();

        foreach (var col in boundsColliders)
        {
            if (col != null)
                result.Add(col.bounds);
        }

        return result;
    }
}