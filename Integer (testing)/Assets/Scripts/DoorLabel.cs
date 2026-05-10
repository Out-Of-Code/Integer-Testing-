using UnityEngine;
using TMPro;

public class DoorLabel : MonoBehaviour
{
    public TMP_Text text;

    public void SetNumber(int index)
    {
        text.text = index.ToString("000");
    }
}