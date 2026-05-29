using System.Collections;
using UnityEngine;

public class InsanityController : MonoBehaviour
{
    public float insanity = 0f;

    public float maxInsanity = 1000f;

    public void AddInsanity(float amount)
    {
        insanity =
            Mathf.Clamp(insanity + amount,
                0,
                maxInsanity);
    }

    public void ReduceInsanity(float amount)
    {
        insanity =
            Mathf.Clamp(insanity - amount,
                0,
                maxInsanity);
    }
    void Start() {
        StartCoroutine(UpdateInstanity(0.2f)); // Run every 2 seconds
    }

    IEnumerator UpdateInstanity(float interval) {
        while (true) {
            AddInsanity(0.1f);
            yield return new WaitForSeconds(interval);
        }
    }

}