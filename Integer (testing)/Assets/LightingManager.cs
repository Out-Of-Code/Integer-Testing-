using UnityEngine;

public class LightingManager : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogDensity = 0.09f;
    }
}