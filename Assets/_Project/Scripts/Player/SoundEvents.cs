using System;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Minimal game-logic sound bus for Part 5.1/5.2's hearing-range mechanic
    /// ("سماع خطوات وفتح أبواب"). Not real spatial audio - just a position + how far
    /// this particular sound carries, so SoundDetector can compare against its own
    /// listener range. Kept as a static event rather than a scene singleton so any
    /// emitter (footsteps, doors) can fire without needing a shared reference.
    /// </summary>
    public static class SoundEvents
    {
        public static event Action<Vector3, float, GameObject> OnSound;

        public static void Emit(Vector3 worldPosition, float audibleRange, GameObject source)
            => OnSound?.Invoke(worldPosition, audibleRange, source);
    }
}
