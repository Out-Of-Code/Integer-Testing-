using UnityEngine;

public class DrawerMoveScript : MonoBehaviour
{
    public Transform Open;
    public Transform Closed;
    public GameObject Drawer;

    public float moveSpeed = 1f;

    private Vector3 targetPosition;

    void Start()
    {
        targetPosition = Closed.position;
    }

    void Update()
    {
        Drawer.transform.position = Vector3.MoveTowards(
            Drawer.transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );
    }

    public void OpenDrawer()
    {
        targetPosition = Open.position;
    }

    public void CloseDrawer()
    {
        targetPosition = Closed.position;
    }
    public void toggleDrawer()
    {
        if (Drawer.transform.position == Closed.position)
        {
            OpenDrawer();
        }
        else
        {
            CloseDrawer();
        }
    }
}