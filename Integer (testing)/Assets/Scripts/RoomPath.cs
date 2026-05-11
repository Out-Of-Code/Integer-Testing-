using System.Collections.Generic;
using UnityEngine;

public class RoomPath : MonoBehaviour
{
    public Transform entry;
    public Transform exit;

    public List<Transform> nodes =
        new();

    public List<Transform> GetFullPath()
    {
        List<Transform> path =
            new();

        path.Add(entry);

        path.AddRange(nodes);

        path.Add(exit);

        return path;
    }
    void Awake()
    {
        entry = transform.Find("Entry");
        exit = transform.Find("Exit");
        nodes.Clear();

        int index = 1;

        while (true)
        {
            Transform node =
                transform.Find($"Node{index}");

            if (node == null)
                break;

            nodes.Add(node);

            index++;
        }
    }
}