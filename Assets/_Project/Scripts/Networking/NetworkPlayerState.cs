using PrankMansion.Player;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PrankMansion.Networking
{
    public enum NetPhysicalState : byte { Normal, Ragdoll, Unconscious, Restrained }

    /// <summary>
    /// Part 10.5's per-player network sync. Position/rotation piggyback on Unity
    /// Netcode's own NetworkTransform component (added alongside this one on the
    /// Player prefab, not reimplemented here - "وفق التوصيات القياسية لنظام
    /// Netcode لأجسام اللاعبين المتحركة"). This component covers the DISCRETE
    /// states 10.5 calls out by name: heavy-carry/wind flag, physical state
    /// (ragdoll/unconscious/restrained), selected character, and display name -
    /// each owner-authoritative (every player syncs their own state outward, which
    /// this project's design already treats as owner-driven: PlayerCarry/
    /// PlayerRagdoll/PlayerCapture/CharacterSelector all run locally on the owning
    /// client). One-shot instant events (slip, flour explosion, ignition, rope-
    /// tie) go through a single ServerRpc/ClientRpc round trip each, matching
    /// "حدث لحظي واحد يُرسَل كحدث شبكي لمرة واحدة" rather than continuous state.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayerState : NetworkBehaviour
    {
        public enum InstantEvent : byte { Slip, FlourExplode, WindIgnite, RopeTie }

        public NetworkVariable<bool> IsHeavyCarrying = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<NetPhysicalState> PhysicalState = new NetworkVariable<NetPhysicalState>(
            NetPhysicalState.Normal, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<int> SelectedCharacterIndex = new NetworkVariable<int>(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<FixedString32Bytes> DisplayName = new NetworkVariable<FixedString32Bytes>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public event System.Action<InstantEvent> OnInstantEventReceived;

        public void RaiseInstantEvent(InstantEvent evt)
        {
            if (!IsOwner) return;
            RaiseInstantEventServerRpc(evt);
        }

        [ServerRpc]
        private void RaiseInstantEventServerRpc(InstantEvent evt) => RaiseInstantEventClientRpc(evt);

        [ClientRpc]
        private void RaiseInstantEventClientRpc(InstantEvent evt) => OnInstantEventReceived?.Invoke(evt);

        public override void OnNetworkDespawn()
        {
            HandleDisconnectCleanup();
            base.OnNetworkDespawn();
        }

        /// Part 10.6: "جسمه أو الغرض الذي كان يحمله وقت المغادرة يتحول فوراً
        /// وتلقائياً لفيزياء طبيعية حرة تماماً" - drop whatever's held so it falls
        /// in place rather than disappearing along with this player's own model
        /// when they disconnect. Public/standalone (rather than inlined in
        /// OnNetworkDespawn) so it's directly testable without needing an active
        /// Netcode session to trigger the real despawn lifecycle callback.
        public void HandleDisconnectCleanup()
        {
            var carry = GetComponent<PlayerCarry>();
            if (carry != null && carry.Held != null) carry.Drop();
        }
    }
}
