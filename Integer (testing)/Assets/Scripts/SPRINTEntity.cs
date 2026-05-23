using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPR_INT : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 50f;
    public float killRadius = 7f;

    [Header("Route")]
    public Queue<RouteNode> Route = new();

    Transform currentTarget;
    RouteNode currentNode;

    bool active;

    // AUDIO
    public AudioSource ambienceSource;
    public AudioSource spawnSource;
    public AudioSource attackSource;

    public AudioClip ambienceClip;
    public AudioClip spawnClip;
    public AudioClip attackClip;
    
    GameManager gameManager;

    // DOOR LOGIC
    Door lastDoor;
    int closedDoorsHit;

    [System.Serializable]
    public class RouteNode
    {
        public Transform point;
        public Door door;
    }

    // =====================================================
    // START
    // =====================================================

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        SetupAudio();
        StartCoroutine(SpawnSequence());
    }

    void SetupAudio()
    {
        if (ambienceSource)
        {
            ambienceSource.clip = ambienceClip;
            ambienceSource.loop = true;
        }

        if (spawnSource) spawnSource.playOnAwake = false;
        if (attackSource) attackSource.playOnAwake = false;
    }

    IEnumerator SpawnSequence()
    {
        Debug.Log("[SPR_INT] Spawn sequence started");

        if (ambienceSource) ambienceSource.Play();

        yield return new WaitForSeconds(3f);
        yield return new WaitForSeconds(4f);

        if (ambienceSource) ambienceSource.Stop();

        if (spawnSource && spawnClip)
        {
            spawnSource.clip = spawnClip;
            spawnSource.Play();
        }

        yield return new WaitForSeconds(3f);

        if (attackSource && attackClip)
        {
            attackSource.clip = attackClip;
            attackSource.Play();
        }

        active = true;

        Debug.Log("[SPR_INT] ACTIVE - starting movement");
        AdvanceTarget();
    }

    // =====================================================
    // UPDATE
    // =====================================================

    void Update()
    {
        if (!active) return;

        Move();
        CheckDoors();
        CheckForPlayers();
    }

    // =====================================================
    // MOVEMENT
    // =====================================================

    void Move()
    {
        if (currentTarget == null)
        {
            Debug.LogWarning("[SPR_INT] No current target (waiting / end of route)");
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            currentTarget.position,
            moveSpeed * Time.deltaTime
        );

        Vector3 dir = currentTarget.position - transform.position;

        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                12f * Time.deltaTime
            );
        }

        if (Vector3.Distance(transform.position, currentTarget.position) < 0.2f)
        {
            Debug.Log("[SPR_INT] Reached node → advancing");
            AdvanceTarget();
        }
    }

    void AdvanceTarget()
    {
        if (Route.Count == 0)
        {
            Debug.LogWarning("[SPR_INT] Route EMPTY - no more movement targets");
            currentTarget = null;
            return;
        }

        currentNode = Route.Dequeue();
        currentTarget = currentNode.point;

        Debug.Log($"[SPR_INT] Next Node: {currentTarget.name} | Door: {(currentNode.door ? currentNode.door.name : "NULL")}");
    }

    // =====================================================
    // DOOR LOGIC
    // =====================================================

    void CheckDoors()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f);

        foreach (Collider hit in hits)
        {
            DoorTrigger trigger = hit.GetComponent<DoorTrigger>();
            if (!trigger) continue;

            Door door = trigger.door;
            if (!door) continue;

            if (door == lastDoor) continue;

            lastDoor = door;

            Debug.Log($"[SPR_INT] Door detected: {door.name}");

            HandleDoor(door);
            return;
        }
    }

    void HandleDoor(Door door)
    {
        bool isOpen = door.IsOpen();

        Debug.Log($"[SPR_INT] Door state → {(isOpen ? "OPEN" : "CLOSED")} | ClosedCount={closedDoorsHit}");

        // OPEN → pass through
        if (isOpen)
        {
            Debug.Log("[SPR_INT] Passing through open door");
            return;
        }

        closedDoorsHit++;

        // FIRST CLOSED → open it
        if (closedDoorsHit == 1)
        {
            Debug.Log("[SPR_INT] First closed door → opening");
            door.Open();
            return;
        }

        // SECOND CLOSED → DIE
        if (closedDoorsHit >= 2)
        {
            Debug.Log("[SPR_INT] SECOND closed door → DESTROY ENTITY");
            Destroy(gameObject);
        }
    }

    // =====================================================
    // ROUTE BUILDING
    // =====================================================

    public void BuildRoute(List<RoomInstance> rooms)
    {
        Route.Clear();

        Debug.Log($"[SPR_INT] Building route with {rooms.Count} rooms");

        int index = 0;

        foreach (RoomInstance room in rooms)
        {
            index++;

            if (room.entries.Count > 0)
            {
                Route.Enqueue(new RouteNode
                {
                    point = room.entries[0],
                    door = null
                });

                Debug.Log($"[SPR_INT] Added entry node for room {index}");
            }

            foreach (var p in room.paths)
            {
                foreach (var node in p.nodes)
                {
                    Route.Enqueue(new RouteNode
                    {
                        point = node,
                        door = null
                    });
                }
            }

            if (room.chosenExit != null)
            {
                Door exitDoor = room.chosenExit.doorSlot.GetComponentInParent<Door>();

                Route.Enqueue(new RouteNode
                {
                    point = room.chosenExit.exitPoint,
                    door = exitDoor
                });

                Debug.Log($"[SPR_INT] Added EXIT door node for room {index} → {(exitDoor ? exitDoor.name : "NULL")}");
            }
        }

        Debug.Log($"[SPR_INT] ROUTE BUILT: {Route.Count} nodes total");

        if (Route.Count > 0)
        {
            currentNode = Route.Dequeue();
            transform.position = currentNode.point.position;
            currentTarget = currentNode.point;

            Debug.Log($"[SPR_INT] Starting at {currentTarget.name}");
        }
    }

    // =====================================================
    // PLAYER CHECK
    // =====================================================

    void CheckForPlayers()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, killRadius);

        foreach (Collider hit in hits)
        {
            SimpleFPSController player = hit.GetComponentInParent<SimpleFPSController>();

            if (player != null)
            {
                if (player.isHidden == false)
                {
                    Debug.Log("[SPR_INT] PLAYER DETECTED (kill logic placeholder)");
                    gameManager.KillPlayer();
                    return;
                }
            }
        }
    }
}