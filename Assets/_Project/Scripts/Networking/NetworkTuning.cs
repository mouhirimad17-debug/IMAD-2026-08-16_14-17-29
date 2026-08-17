namespace PrankMansion.Networking
{
    /// <summary>
    /// Part 15.3: "يُقترح ضبط معدل تحديث الموقع والدوران عند عشرين تحديثاً في
    /// الثانية تقريباً". No NetworkManager/transport exists in a scene yet (Stage
    /// 15 deferred that whole layer pending real Steamworks.NET, per
    /// Stage15NetworkingSetup's own decisions log) - Unity Netcode's per-object
    /// NetworkTransform doesn't expose a per-component send rate, only the global
    /// NetworkManager.NetworkConfig.TickRate. This constant is the value to apply
    /// there the moment that scene-level setup happens; recorded now so the
    /// number itself isn't lost or re-guessed later.
    /// </summary>
    public static class NetworkTuning
    {
        public const int PositionSyncRateHz = 20;
    }
}
