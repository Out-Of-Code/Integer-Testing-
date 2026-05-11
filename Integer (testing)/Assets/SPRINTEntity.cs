using UnityEngine;

public class SPRINTEntity : MonoBehaviour
{
    public SPRINTController controller;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (controller == null)
            {
            controller = FindObjectOfType<SPRINTController>();
            }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
