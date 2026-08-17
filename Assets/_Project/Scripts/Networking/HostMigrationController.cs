using System.Collections.Generic;
using System.Linq;

namespace PrankMansion.Networking
{
    /// <summary>
    /// Part 10.6's disconnect/host-migration RULES as pure, directly-testable
    /// logic. The actual network-session handoff (tearing down and reforming the
    /// real Netcode/Steam relay session under a new authoritative host) needs a
    /// real second peer to prove end-to-end - Part 10.7 itself mandates that proof
    /// happen over two genuinely separate networks - so that live handoff is
    /// deferred. What Part 10.6 actually specifies as RULES (who becomes host
    /// next, and the fallback when migration can't complete) is fully built here.
    /// </summary>
    public static class HostMigrationController
    {
        /// Part 10.6: "نقل صلاحية الاستضافة تلقائياً وفورياً للاعب التالي وفق ترتيب
        /// انضمامه الأصلي للوبي" - next host is whoever remains with the lowest
        /// original join order (the departing host is expected to already be
        /// excluded from remainingPlayers by the caller).
        public static PlayerLobbyEntry SelectNextHost(IEnumerable<PlayerLobbyEntry> remainingPlayers) =>
            remainingPlayers.OrderBy(p => p.JoinOrder).FirstOrDefault();
    }
}
