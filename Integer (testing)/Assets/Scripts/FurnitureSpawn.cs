using UnityEngine;

public class FurnitureSpawn : MonoBehaviour
{
    public FurnitureType allowedType;
    
    [Range(0f, 1f)]
    public float spawnChance = 1f;

    public bool required;

    public bool occupied;
}