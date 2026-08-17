using UnityEngine;

namespace PrankMansion.Systems
{
    /// <summary>
    /// Law 0.2 placeholder generator for audio: Assets/_Project/Audio/SFX/Fart and
    /// .../Physics are empty (no real sound files imported yet), so the wind loop and
    /// ignition-explosion cues in Part 7.2 need a stand-in clip to be built and
    /// tested now rather than blocking on final audio assets. Generates a short
    /// procedural sine tone entirely in code (no missing-file dependency) and logs
    /// both the missing source and the generated substitute per Law 0.2's two-step
    /// process.
    /// </summary>
    public static class PlaceholderAudio
    {
        public static AudioClip GenerateTone(string clipName, float frequencyHz, float durationSeconds, float volume = 0.2f)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * durationSeconds));
            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);

            var data = new float[sampleCount];
            float fadeSeconds = Mathf.Min(0.02f, durationSeconds * 0.25f);
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float fadeIn = fadeSeconds > 0f ? Mathf.Clamp01(t / fadeSeconds) : 1f;
                float fadeOut = fadeSeconds > 0f ? Mathf.Clamp01((durationSeconds - t) / fadeSeconds) : 1f;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequencyHz * t) * volume * Mathf.Min(fadeIn, fadeOut);
            }
            clip.SetData(data, 0);
            return clip;
        }
    }
}
