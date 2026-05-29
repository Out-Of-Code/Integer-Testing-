using UnityEngine;

public class SanityTextCorruptor : MonoBehaviour, ISanityAffected
{
    [Header("Settings")]
    public float corruptionThreshold = 200f;

    private float currentInsanity;
    private float maxInsanity = 1000f;

    // ISanityAffected implementation
    public void OnSanityChanged(float insanity, float max)
    {
        currentInsanity = insanity;
        maxInsanity = max;
    }

    // Instance wrapper — call on a component reference if needed
    public string Corrupt(string input)
    {
        return CorruptText(input, currentInsanity, maxInsanity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // STATIC HELPER — use anywhere without a component reference
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a corrupted version of <paramref name="input"/> scaled by insanity.
    /// insanity 0 → no corruption. insanity == maxInsanity → heavy corruption.
    /// </summary>
    public static string CorruptText(string input, float insanity, float maxInsanity)
    {
        if (string.IsNullOrEmpty(input)) return input;
        if (maxInsanity <= 0f) return input;

        float t = Mathf.Clamp01(insanity / maxInsanity);

        // No corruption below threshold ratio
        if (t < 0.2f) return input;

        char[] chars = input.ToCharArray();
        int corruptCount = Mathf.FloorToInt(chars.Length * t * 0.6f);

        for (int i = 0; i < corruptCount; i++)
        {
            int index = Random.Range(0, chars.Length);
            chars[index] = GetCorruptChar(t);
        }

        // At very high insanity, inject glitch strings
        string result = new string(chars);
        if (t > 0.7f)
            result = InjectGlitchFragments(result, t);

        // ENTITY CORRUPTION EXTENSION POINT:
        // Pass entity-specific corruption seeds here to make each entity's
        // text corruption visually distinct (e.g. one entity uses symbols,
        // another uses binary, another uses reversed text).

        return result;
    }

    static char GetCorruptChar(float intensity)
    {
        // Low intensity: punctuation noise
        // High intensity: binary / symbol chaos
        if (intensity < 0.5f)
        {
            char[] light = { '#', '%', '&', '?', '!', '@', '*', '~', '^' };
            return light[Random.Range(0, light.Length)];
        }
        else
        {
            char[] heavy = { '0', '1', '\u2588', '\u2593', '\u2592', '\u2591', '\u00A7', '\u00B6', '\u25A0', '\u25AA' };
            return heavy[Random.Range(0, heavy.Length)];
        }
    }

    static string InjectGlitchFragments(string input, float intensity)
    {
        string[] fragments = { "̷̢", "ERR", "NULL", "0x00", "CORRUPT", "̴̡", "???", "VOID" };
        int injectCount = Mathf.FloorToInt(intensity * 2f);
        System.Text.StringBuilder sb = new System.Text.StringBuilder(input);
        for (int i = 0; i < injectCount; i++)
        {
            int pos = Random.Range(0, sb.Length);
            string frag = fragments[Random.Range(0, fragments.Length)];
            sb.Insert(pos, frag);
        }
        return sb.ToString();
    }
}
