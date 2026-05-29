using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// SanityTextCorruptor.cs
// Utility MonoBehaviour that distorts text based on the player's current insanity level.
// Implements ISanityAffected — attach to any GameObject that needs sanity-reactive text.
//
// Usage:
//   1. Attach to a GameObject in the scene (or on the computer UI canvas).
//   2. Call Corrupt(string cleanText) to get a distorted version of the text.
//   3. Or call StartLiveCorruption(TMP_Text target, string cleanText) to animate
//      the corruption in real time while the player reads.
//
// Corruption tiers (based on insanity / maxInsanity ratio):
//   0.00 – 0.30  → Clean (no distortion)
//   0.30 – 0.55  → Light (occasional character swap, rare word drop)
//   0.55 – 0.75  → Moderate (frequent swaps, missing words, glitch chars)
//   0.75 – 0.90  → Heavy (half the text is noise, words reversed)
//   0.90 – 1.00  → Severe (mostly unreadable, only fragments survive)
//
// Place in: Assets/Scripts/

public class SanityTextCorruptor : MonoBehaviour, ISanityAffected
{
    [Header("Corruption Settings")]
    public float lightThreshold = 0.30f;
    public float moderateThreshold = 0.55f;
    public float heavyThreshold = 0.75f;
    public float severeThreshold = 0.90f;

    // Characters used to replace or inject during corruption
    static readonly char[] glitchChars = new char[]
    {
        '#', '%', '&', '@', '!', '?', '/', '\\', '|', '_',
        '0', '1', 'X', 'Z', '\u2588', '\u2593', '\u2592'
    };

    // Cached sanity ratio — updated via OnSanityChanged or polled in Update
    float sanityratio;

    InsanityController playerInsanity;

    void Update()
    {
        // Poll pattern — consistent with how ComputerController reads InsanityController.
        if (playerInsanity == null)
            playerInsanity = FindObjectOfType<InsanityController>();

        if (playerInsanity != null)
        {
            float newRatio = playerInsanity.insanity / playerInsanity.maxInsanity;
            if (Mathf.Abs(newRatio - sanityratio) > 0.01f)
            {
                sanityratio = newRatio;
                OnSanityChanged(playerInsanity.insanity, playerInsanity.maxInsanity);
            }
        }
    }

    // ISanityAffected implementation
    public void OnSanityChanged(float insanity, float maxInsanity)
    {
        sanityratio = maxInsanity > 0f ? insanity / maxInsanity : 0f;
    }

    // Returns a corrupted version of cleanText based on current sanity ratio.
    // Pure function — does not modify any UI directly.
    public string Corrupt(string cleanText)
    {
        if (string.IsNullOrEmpty(cleanText)) return cleanText;

        if (sanityratio < lightThreshold)
            return cleanText;
        if (sanityratio < moderateThreshold)
            return ApplyLightCorruption(cleanText);
        if (sanityratio < heavyThreshold)
            return ApplyModerateCorruption(cleanText);
        if (sanityratio < severeThreshold)
            return ApplyHeavyCorruption(cleanText);

        return ApplySevereCorruption(cleanText);
    }

    // Convenience: corrupt using an explicit ratio (for preview/testing).
    public string CorruptAtRatio(string cleanText, float ratio)
    {
        float saved = sanityratio;
        sanityratio = ratio;
        string result = Corrupt(cleanText);
        sanityratio = saved;
        return result;
    }

    // --- Corruption tiers ---

    string ApplyLightCorruption(string text)
    {
        // ~8% of characters swapped, ~5% of words dropped
        char[] chars = text.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] == ' ' || chars[i] == '\n') continue;
            if (Random.value < 0.08f)
                chars[i] = glitchChars[Random.Range(0, glitchChars.Length)];
        }
        return DropWords(new string(chars), 0.05f);
    }

    string ApplyModerateCorruption(string text)
    {
        // ~20% char swap, ~15% word drop, occasional word reversal
        char[] chars = text.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] == ' ' || chars[i] == '\n') continue;
            if (Random.value < 0.20f)
                chars[i] = glitchChars[Random.Range(0, glitchChars.Length)];
        }
        string result = DropWords(new string(chars), 0.15f);
        return ReverseRandomWords(result, 0.10f);
    }

    string ApplyHeavyCorruption(string text)
    {
        // ~45% char swap, ~30% word drop, ~20% word reversal, inject noise lines
        char[] chars = text.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] == '\n') continue;
            if (Random.value < 0.45f)
                chars[i] = chars[i] == ' ' ? ' ' : glitchChars[Random.Range(0, glitchChars.Length)];
        }
        string result = DropWords(new string(chars), 0.30f);
        result = ReverseRandomWords(result, 0.20f);
        return InjectNoise(result, 0.15f);
    }

    string ApplySevereCorruption(string text)
    {
        // ~70% char swap, ~55% word drop — only fragments survive
        char[] chars = text.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] == '\n') continue;
            if (Random.value < 0.70f)
                chars[i] = chars[i] == ' ' ? ' ' : glitchChars[Random.Range(0, glitchChars.Length)];
        }
        string result = DropWords(new string(chars), 0.55f);
        return InjectNoise(result, 0.30f);
    }

    // --- Helpers ---

    // Randomly drops words from the text at the given probability.
    string DropWords(string text, float dropChance)
    {
        string[] words = text.Split(' ');
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < words.Length; i++)
        {
            if (Random.value < dropChance)
            {
                // Replace dropped word with underscores of same length
                sb.Append(new string('_', Mathf.Max(1, words[i].Length)));
            }
            else
            {
                sb.Append(words[i]);
            }
            if (i < words.Length - 1) sb.Append(' ');
        }
        return sb.ToString();
    }

    // Randomly reverses individual words.
    string ReverseRandomWords(string text, float reverseChance)
    {
        string[] words = text.Split(' ');
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 2 && Random.value < reverseChance)
            {
                char[] arr = words[i].ToCharArray();
                System.Array.Reverse(arr);
                sb.Append(new string(arr));
            }
            else
            {
                sb.Append(words[i]);
            }
            if (i < words.Length - 1) sb.Append(' ');
        }
        return sb.ToString();
    }

    // Injects random glitch character sequences between words.
    string InjectNoise(string text, float noiseChance)
    {
        string[] words = text.Split(' ');
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < words.Length; i++)
        {
            sb.Append(words[i]);
            if (i < words.Length - 1)
            {
                if (Random.value < noiseChance)
                {
                    int len = Random.Range(2, 6);
                    sb.Append(' ');
                    for (int j = 0; j < len; j++)
                        sb.Append(glitchChars[Random.Range(0, glitchChars.Length)]);
                }
                sb.Append(' ');
            }
        }
        return sb.ToString();
    }
}
 